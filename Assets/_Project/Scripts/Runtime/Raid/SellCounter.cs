using Player;
using System.Collections.Generic;
using Code.Scripts.EventSystems;
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
        [Tooltip("The floor in front of the counter, on the player's side: a piece too heavy to lift alone is towed here to sell (#356).")]
        [SerializeField] private BoxCollider _foot;
        [SerializeField] private Renderer _figure;
        [Tooltip("The chalk slate on the counter that shows his name, his words and the keys.")]
        [SerializeField] private CounterSlate _slate;
        [Tooltip("How close the local player must stand to answer, in metres.")]
        [SerializeField] private float _reach = 3f;
        [Tooltip("The coin pouch a sale puts on the counter.")]
        [SerializeField] private CoinPouch _pouchPrefab;

        private const float RestingSpeed = 0.2f;
        private const float LineSeconds = 6f;

        private Haggle _haggle;
        private GameObject _piece;
        private int _nightSeed;
        private float _clearAt;
        private string _line = ""; // his last line, on every side; empty when the slate shows what he wants
        private bool _open;        // whether that line came with the haggle still open

        // Pieces he refused for the rest of the night, and pieces the player said Vale over (instance ids).
        private readonly HashSet<int> _refused = new HashSet<int>();
        private readonly HashSet<int> _walkedAway = new HashSet<int>();

        public Vendor Vendor => _vendor;
        public Haggle Open => _haggle;

        /// <summary>The wiring the Market prefab's builder does, in one call.</summary>
        public void Set(Vendor vendor, BoxCollider top, BoxCollider foot, Renderer figure, CounterSlate slate)
        {
            _vendor = vendor;
            _top = top;
            _foot = foot;
            _figure = figure;
            _slate = slate;
        }

        private void Awake()
        {
            NewNight();
            if (_figure != null)
                _figure.material.color = VendorLines.Colour(_vendor);
            _slate?.Show(SlateText.For(_vendor, false, ""));
        }

        // A night runs from setting out to setting out: a new raid forgets who walked away and rolls a new mood.
        private void OnEnable()
        {
            All.Add(this);
            HaggleVoiceRouter.Ensure();
            EventManager.Instance?.Subscribe(this, (RaidPhaseChanged e) =>
            {
                if (e.Phase == RaidPhase.Generating)
                    NewNight();
            });
        }

        private void OnDisable()
        {
            All.Remove(this);
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
        }

        /// <summary>Every enabled counter, for <see cref="HaggleVoiceRouter"/>.</summary>
        public static readonly List<SellCounter> All = new List<SellCounter>();

        /// <summary>
        /// Metres from the local player when a spoken word would reach this counter (player in reach and a haggle open;
        /// a client only sees the vendor's line, as <see cref="Update"/> does), else infinity.
        /// </summary>
        public float ListeningDistance()
        {
            bool open = Decides ? _haggle != null : _line != "";
            Camera eye = Camera.main;
            if (!open || eye == null || _top == null)
                return float.PositiveInfinity;
            float distance = Vector3.Distance(eye.transform.position, _top.bounds.center);
            return distance <= _reach ? distance : float.PositiveInfinity;
        }

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
            if (_slate != null && _clearAt > 0f && Time.time > _clearAt && !_open)
            {
                _line = "";
                _slate.Show(SlateText.For(_vendor, false, ""));
                _clearAt = 0f;
            }
            if (!Decides)
            {
                if (PlayerIsNear() && _line != "")
                    ReadKeys(); // the server knows if a haggle is open; this side only sees its lines
                return;
            }

            if (_haggle != null && (_piece == null || !Rests(_piece)))
            {
                _haggle = null; // taken off the counter without a word
                Announce(_line, false);
            }

            if (_haggle == null)
                OpenOnRestingPiece();
            else if (PlayerIsNear())
                ReadKeys();
        }

        private void ReadKeys()
        {
            if (GameInput.Actions.Haggle.Plus.WasPressedThisFrame()) Speak(HaggleWord.Plus);
            else if (GameInput.Actions.Haggle.Satis.WasPressedThisFrame()) Speak(HaggleWord.Satis);
            else if (GameInput.Actions.Haggle.Vale.WasPressedThisFrame()) Speak(HaggleWord.Vale);
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
            foreach (BoxCollider zone in new[] { _top, _foot })
            {
                if (zone == null)
                    continue;
                foreach (Collider hit in Physics.OverlapBox(zone.bounds.center, zone.size * 0.5f, zone.transform.rotation,
                             Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    LootValue value = hit.GetComponentInParent<LootValue>();
                    if (value == null || _refused.Contains(value.gameObject.GetInstanceID()) || !Rests(value.gameObject))
                        continue;
                    Begin(value);
                    return;
                }
            }
        }

        private static bool Inside(BoxCollider zone, Vector3 point)
        {
            if (zone == null)
                return false;
            Vector3 local = zone.transform.InverseTransformPoint(point) - zone.center;
            return Mathf.Abs(local.x) <= zone.size.x * 0.5f && Mathf.Abs(local.y) <= zone.size.y * 0.5f && Mathf.Abs(local.z) <= zone.size.z * 0.5f;
        }

        private bool Rests(GameObject piece)
        {
            if (piece.TryGetComponent(out LootPickup pickup) && pickup.IsBeingCarried)
                return false;
            if (!Inside(_top, piece.transform.position) && !Inside(_foot, piece.transform.position))
                return false;
            return !piece.TryGetComponent(out Rigidbody body) || body.isKinematic || body.linearVelocity.sqrMagnitude < RestingSpeed * RestingSpeed;
        }

        private void Begin(LootValue value)
        {
            int id = value.gameObject.GetInstanceID();
            // worth = LootValue.Worth (the item's Worth, 0 once ruined: a LootValue has no partial condition).
            // interest = this vendor's liking for the piece's LootItem.Category (HaggleRules.Interest); no item: Other.
            float worth = value.Worth;
            if (worth <= 0f)
            {
                _refused.Add(id);
                Say(VendorLines.Worthless(_vendor));
                return;
            }

            LootCategory category = value.Item != null ? value.Item.Category : LootCategory.Other;
            float interest = HaggleRules.Interest(_vendor, category);
            float mood = HaggleRules.RollMood(new System.Random(_nightSeed + (int)_vendor));
            // The same seed for the same piece all night, so coming back opens exactly 10% lower.
            var rng = new System.Random(_nightSeed ^ id);
            _haggle = HaggleRules.Open(_vendor, worth, interest, mood, rng, _walkedAway.Contains(id));
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
            PutPouchOnCounter(coins);
            // Out of the pile first so its save drops the piece, then out of any raid spawner's list.
            FindFirstObjectByType<HaulLanding>()?.Remove(piece);
            foreach (LootSpawner spawner in FindObjectsByType<LootSpawner>(FindObjectsSortMode.None))
                spawner.Remove(piece);
            Destroy(piece); // on a spawned piece the server's destroy despawns it for every client
        }

        // The coins come as a pouch, banked only when someone carries it to a strongbox (LairStrongbox).
        private void PutPouchOnCounter(int coins)
        {
            if (_pouchPrefab == null)
            {
                Debug.LogWarning($"[Market] {name} has no pouch prefab, so the {coins} coins of this sale are lost.");
                return;
            }
            Vector3 at = _top.bounds.center + Vector3.up * 0.15f;
            CoinPouch pouch = Instantiate(_pouchPrefab, at, _pouchPrefab.transform.rotation);
            pouch.Fill(coins);
        }

        private void Say(string line) => Announce(line, _haggle != null && !_haggle.IsOver);

        // The one path to every player's slate: the line, and whether the haggle is still open (for the keys under it).
        private void Announce(string line, bool open)
        {
            if (_slate == null)
                return;
            Show(line, open);
            if (isSpawned && isServer)
                LineToObservers(line, open);
        }

        [ObserversRpc]
        private void LineToObservers(string line, bool open)
        {
            if (!isServer) // the host showed it in Announce
                Show(line, open);
        }

        private void Show(string line, bool open)
        {
            _line = line;
            _open = open;
            _slate.Show(SlateText.For(_vendor, open, line));
            _clearAt = Time.time + LineSeconds;
        }
    }
}
