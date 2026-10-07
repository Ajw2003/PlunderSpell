using System.Collections.Generic;
using Code.Scripts.EventSystems;
using Plunderspell.Lair;
using Plunderspell.Loot;
using Plunderspell.Market;
using PurrNet;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// One Market counter and its vendor. A loot piece resting on the counter's trigger volume opens a haggle;
    /// the local player answers within reach with 1 Plus, 2 Satis, 3 Vale. The server owns the haggle and the sale; a client's
    /// word goes to it by <see cref="WordToServer"/> and every line reaches every player by <see cref="LineToObservers"/>.
    /// Unspawned (a scene without a session) the counter is its own authority. See docs/4-systems/market.md.
    /// </summary>
    public class SellCounter : NetworkBehaviour
    {
        [SerializeField] private Vendor _vendor;
        [Tooltip("The counter top: a piece resting inside it is offered to the vendor.")]
        [SerializeField] private BoxCollider _top;
        [SerializeField] private Renderer _figure;
        [SerializeField] private TextMesh _subtitle;
        [Tooltip("How close the local player must stand to answer, in metres.")]
        [SerializeField] private float _reach = 3f;

        private const float RestingSpeed = 0.2f;
        private const float LineSeconds = 6f;

        private Haggle _haggle;
        private GameObject _piece;
        private int _nightSeed;
        private float _clearAt;

        // Pieces he refused for the rest of the night, and pieces the player said Vale over (instance ids).
        private readonly HashSet<int> _refused = new HashSet<int>();
        private readonly HashSet<int> _walkedAway = new HashSet<int>();

        public Vendor Vendor => _vendor;
        public Haggle Open => _haggle;

        /// <summary>The wiring the Market prefab's builder does, in one call.</summary>
        public void Set(Vendor vendor, BoxCollider top, Renderer figure, TextMesh subtitle)
        {
            _vendor = vendor;
            _top = top;
            _figure = figure;
            _subtitle = subtitle;
        }

        private void Awake()
        {
            NewNight();
            if (_figure != null)
                _figure.material.color = VendorLines.Colour(_vendor);
            if (_subtitle != null)
            {
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _subtitle.font = font;
                _subtitle.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                _subtitle.text = "";
            }
        }

        // A night runs from setting out to setting out: a new raid forgets who walked away and rolls a new mood.
        private void OnEnable() => EventManager.Instance?.Subscribe(this, (RaidPhaseChanged e) =>
        {
            if (e.Phase == RaidPhase.Generating)
                NewNight();
        });

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        private void NewNight()
        {
            _nightSeed = Random.Range(int.MinValue, int.MaxValue);
            _refused.Clear();
            _walkedAway.Clear();
            _haggle = null;
            _piece = null;
        }

        // Unspawned is its own authority; PurrNet's [ServerRpc] does nothing on an unspawned object.
        private bool Decides => !isSpawned || isServer;

        private void Update()
        {
            if (_subtitle != null && _clearAt > 0f && Time.time > _clearAt && _haggle == null)
            {
                _subtitle.text = "";
                _clearAt = 0f;
            }
            if (!Decides)
            {
                if (PlayerIsNear() && _subtitle != null && _subtitle.text != "")
                    ReadKeys(); // the server knows if a haggle is open; this side only sees its lines
                return;
            }

            if (_haggle != null && (_piece == null || !Rests(_piece)))
                _haggle = null; // taken off the counter without a word

            if (_haggle == null)
                OpenOnRestingPiece();
            else if (PlayerIsNear())
                ReadKeys();
        }

        private void LateUpdate()
        {
            Camera eye = Camera.main;
            if (_subtitle != null && eye != null)
                _subtitle.transform.rotation = Quaternion.LookRotation(_subtitle.transform.position - eye.transform.position);
        }

        private void ReadKeys()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Speak(HaggleWord.Plus);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) Speak(HaggleWord.Satis);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) Speak(HaggleWord.Vale);
        }

        /// <summary>The local player's word: answered here when this side decides, else sent to the server. The keys call this.</summary>
        public void Speak(HaggleWord word)
        {
            if (Decides)
                Answer(word);
            else
                WordToServer((int)word);
        }

        [ServerRpc(requireOwnership: false)]
        private void WordToServer(int word) => Answer((HaggleWord)word);

        private bool PlayerIsNear()
        {
            Camera eye = Camera.main;
            return eye != null && Vector3.Distance(eye.transform.position, _top.bounds.center) <= _reach;
        }

        /// <summary>The first piece lying still on the counter that he has not refused tonight.</summary>
        private void OpenOnRestingPiece()
        {
            foreach (Collider hit in Physics.OverlapBox(_top.bounds.center, _top.size * 0.5f, _top.transform.rotation,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                LootValue value = hit.GetComponentInParent<LootValue>();
                if (value == null || _refused.Contains(value.gameObject.GetInstanceID()) || !Rests(value.gameObject))
                    continue;
                Begin(value);
                return;
            }
        }

        private bool Rests(GameObject piece)
        {
            if (piece.TryGetComponent(out LootPickup pickup) && pickup.IsBeingCarried)
                return false;
            Vector3 local = _top.transform.InverseTransformPoint(piece.transform.position) - _top.center;
            if (Mathf.Abs(local.x) > _top.size.x * 0.5f || Mathf.Abs(local.y) > _top.size.y * 0.5f || Mathf.Abs(local.z) > _top.size.z * 0.5f)
                return false;
            return !piece.TryGetComponent(out Rigidbody body) || body.isKinematic || body.linearVelocity.sqrMagnitude < RestingSpeed * RestingSpeed;
        }

        private void Begin(LootValue value)
        {
            int id = value.gameObject.GetInstanceID();
            // worth = LootValue.Worth (the item's Worth, 0 once ruined: a LootValue has no partial condition).
            // interest = 1: LootItem has no category or material to map, so every vendor wants every piece equally for now.
            float worth = value.Worth;
            if (worth <= 0f)
            {
                _refused.Add(id);
                Say(VendorLines.Worthless(_vendor));
                return;
            }

            float mood = HaggleRules.RollMood(new System.Random(_nightSeed + (int)_vendor));
            // The same seed for the same piece all night, so coming back opens exactly 10% lower.
            var rng = new System.Random(_nightSeed ^ id);
            _haggle = HaggleRules.Open(_vendor, worth, 1f, mood, rng, _walkedAway.Contains(id));
            _piece = value.gameObject;
            Say(VendorLines.Opening(_vendor, Coins()));
        }

        private int Coins() => Mathf.Max(1, Mathf.RoundToInt(_haggle.Offer));

        /// <summary>The player's answer to the open haggle. Server only; the keys call this. Null when nothing is open.</summary>
        public HaggleOutcome? Answer(HaggleWord word)
        {
            if (_haggle == null)
                return null;

            HaggleOutcome outcome = _haggle.Answer(word);
            GameObject piece = _piece;
            int coins = Coins();
            switch (outcome)
            {
                case HaggleOutcome.Raised: Say(VendorLines.Raised(_vendor, coins)); break;
                case HaggleOutcome.Refused: Say(VendorLines.Refused(_vendor)); break;
                case HaggleOutcome.WillNotBuy:
                    _refused.Add(piece.GetInstanceID());
                    Say(VendorLines.WillNotBuy(_vendor));
                    break;
                case HaggleOutcome.WalkedAway:
                    _walkedAway.Add(piece.GetInstanceID());
                    Say(VendorLines.Farewell(_vendor));
                    break;
                case HaggleOutcome.Sold:
                    Say(VendorLines.Sold(_vendor, coins));
                    Sell(piece, coins);
                    break;
            }
            if (_haggle.IsOver)
            {
                _haggle = null;
                _piece = null;
            }
            return outcome;
        }

        private void Sell(GameObject piece, int coins)
        {
            FindFirstObjectByType<LairHubManager>()?.BankSale(coins);
            // Out of the pile first so its save drops the piece, then out of any raid spawner's list.
            FindFirstObjectByType<HaulLanding>()?.Remove(piece);
            foreach (LootSpawner spawner in FindObjectsByType<LootSpawner>(FindObjectsSortMode.None))
                spawner.Remove(piece);
            Destroy(piece); // on a spawned piece the server's destroy despawns it for every client
        }

        private void Say(string line)
        {
            if (_subtitle == null)
                return;
            Show(line);
            if (isSpawned && isServer)
                LineToObservers(line);
        }

        [ObserversRpc]
        private void LineToObservers(string line)
        {
            if (!isServer) // the host showed it in Say
                Show(line);
        }

        private void Show(string line)
        {
            _subtitle.text = line;
            _clearAt = Time.time + LineSeconds;
        }
    }
}
