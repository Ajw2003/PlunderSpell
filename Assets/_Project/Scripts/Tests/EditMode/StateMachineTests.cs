using System.Collections.Generic;
using NUnit.Framework;
using StateMachine;

namespace Plunderspell.Tests.EditMode
{
    /// <summary>Tests for the plain C# <see cref="StateMachine{TContext}"/>.</summary>
    public class StateMachineTests
    {
        private sealed class Probe : State<List<string>>
        {
            private readonly string _name;
            public State<List<string>> NextOnTick;

            public Probe(List<string> log, string name) : base(log)
            {
                _name = name;
            }

            public override void Enter() { Context.Add(_name + ".Enter"); }
            public override void Exit() { Context.Add(_name + ".Exit"); }

            public override State<List<string>> Tick(float deltaTime)
            {
                Context.Add(_name + ".Tick");
                return NextOnTick ?? this;
            }

            public override void FixedTick(float deltaTime) { Context.Add(_name + ".Fixed"); }
        }

        private List<string> _log;
        private StateMachine<List<string>> _machine;
        private Probe _a;
        private Probe _b;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _machine = new StateMachine<List<string>>();
            _a = new Probe(_log, "A");
            _b = new Probe(_log, "B");
        }

        [Test]
        public void ChangeState_ExitsOldThenEntersNew()
        {
            _machine.ChangeState(_a);
            _machine.ChangeState(_b);

            Assert.AreEqual(new[] { "A.Enter", "A.Exit", "B.Enter" }, _log);
            Assert.AreSame(_b, _machine.Current);
        }

        [Test]
        public void ChangeState_SameState_DoesNothing()
        {
            _machine.ChangeState(_a);
            _log.Clear();
            _machine.ChangeState(_a);

            Assert.IsEmpty(_log);
        }

        [Test]
        public void Tick_ReturningAnotherState_Transitions()
        {
            _machine.ChangeState(_a);
            _a.NextOnTick = _b;
            _log.Clear();
            _machine.Tick(0.1f);

            Assert.AreEqual(new[] { "A.Tick", "A.Exit", "B.Enter" }, _log);
            Assert.AreSame(_b, _machine.Current);
        }

        [Test]
        public void Tick_ReturningItself_Stays()
        {
            _machine.ChangeState(_a);
            _log.Clear();
            _machine.Tick(0.1f);

            Assert.AreEqual(new[] { "A.Tick" }, _log);
        }

        [Test]
        public void FixedTick_RunsCurrentState()
        {
            _machine.ChangeState(_a);
            _log.Clear();
            _machine.FixedTick(0.02f);

            Assert.AreEqual(new[] { "A.Fixed" }, _log);
        }

        [Test]
        public void StateChanged_FiresWithFromAndTo()
        {
            var seen = new List<(State<List<string>>, State<List<string>>)>();
            _machine.StateChanged += (from, to) => seen.Add((from, to));

            _machine.ChangeState(_a);
            _machine.ChangeState(_b);
            _machine.ChangeState(_b);

            Assert.AreEqual(2, seen.Count);
            Assert.IsNull(seen[0].Item1);
            Assert.AreSame(_a, seen[0].Item2);
            Assert.AreSame(_a, seen[1].Item1);
            Assert.AreSame(_b, seen[1].Item2);
        }
    }
}
