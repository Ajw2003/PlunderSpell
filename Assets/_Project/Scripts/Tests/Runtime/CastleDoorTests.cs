using NUnit.Framework;
using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>A door off the network (solo, tests) keeps its rules, and the hinge follows the synced open state (#248).</summary>
    public class CastleDoorTests
    {
        private GameObject _go;
        private Transform _hinge;
        private CastleDoor _door;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Door");
            _go.SetActive(false); // so the hinge is assigned before Awake reads it
            _hinge = new GameObject("Hinge").transform;
            _hinge.SetParent(_go.transform, false);
            _door = _go.AddComponent<CastleDoor>();
            var serialized = new SerializedObject(_door);
            serialized.FindProperty("_hinge").objectReferenceValue = _hinge;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _go.SetActive(true);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void HingeSwingsWhenTheOpenStateChangesAndFiresTheEvent()
        {
            bool? heard = null;
            _door.OpenStateChanged += open => heard = open;

            _door.Open();
            Assert.IsTrue(_door.IsOpen);
            Assert.AreEqual(90f, Quaternion.Angle(Quaternion.identity, _hinge.localRotation), 0.01f);
            Assert.AreEqual(true, heard);

            _door.Close();
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, _hinge.localRotation), 0.01f);
            Assert.AreEqual(false, heard);
        }

        [Test]
        public void OfflineLockAndBarRulesHold()
        {
            Assert.IsTrue(_door.TryOpenByHand(), "unlocked opens by hand");
            _door.Close();

            _door.Lock();
            Assert.IsTrue(_door.IsLocked);
            Assert.IsFalse(_door.TryOpenByHand(), "locked refuses a hand");

            _door.Unlock();
            _door.Bar();
            Assert.IsTrue(_door.IsBarred);
            Assert.IsFalse(_door.TryOpenByHand(), "barred refuses a hand");
            Assert.IsTrue(_door.ForceOpen(), "forcing always works");
        }

        [Test]
        public void TheHandClosesAnOpenDoor()
        {
            var handle = _go.AddComponent<Plunderspell.Loot.CastleDoorHandle>();
            handle.SetDoor(_door);

            Assert.IsTrue(handle.Interact(), "opens");
            Assert.IsTrue(_door.IsOpen);
            Assert.IsTrue(handle.Interact(), "closes again (#276)");
            Assert.IsFalse(_door.IsOpen);
        }

        // A door faces into the room it leads to; at a stair head its back is the stair, so the open leaf must
        // land on the front side, never across the steps (#276).
        [TestCase("CastleDoor_InnerWard")]
        [TestCase("CastleDoor_Keep")]
        [TestCase("CastleDoor_Crypt")]
        public void AForgedDoorSwingsIntoTheRoomItFaces(string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Castle/{prefabName}.prefab");
            GameObject door = Object.Instantiate(prefab);
            try
            {
                door.GetComponent<CastleDoor>().Open();
                Vector3 leaf = door.transform.InverseTransformPoint(door.GetComponentInChildren<Renderer>().bounds.center);
                Assert.Greater(leaf.z, 0.5f, $"open leaf centre at local {leaf}");
            }
            finally
            {
                Object.DestroyImmediate(door);
            }
        }
    }
}
