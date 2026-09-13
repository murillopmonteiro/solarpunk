using NUnit.Framework;
using Solarpunk.Core;
using Solarpunk.Managers;
using UnityEngine;

namespace Solarpunk.Tests
{
    public class ResourceManagerTests
    {
        private GameObject _host;
        private ResourceManager _resources;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("TestHost");
            _resources = _host.AddComponent<ResourceManager>();
            _resources.Initialize();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_host);

        [Test]
        public void ApplyTurnAccumulatesEachResource()
        {
            float startingMoney = _resources.Current.money;
            _resources.ApplyTurn(new ResourceVector { money = 15f, population = 5f });

            Assert.AreEqual(startingMoney + 15f, _resources.Current.money, 0.001f);
            Assert.AreEqual(15f, _resources.Current.population, 0.001f);
        }

        [Test]
        public void SurplusEnergyCarriesOverAsABuffer()
        {
            // Design doc: there is no storage tile; the energy stat is itself the buffer.
            _resources.ApplyTurn(new ResourceVector { energy = 30f });
            _resources.ApplyTurn(new ResourceVector { energy = -10f });

            Assert.AreEqual(40f, _resources.Current.energy, 0.001f);
        }

        [Test]
        public void EnergyDeficitCostsHappinessAndFloorsAtZero()
        {
            float startingHappiness = _resources.Current.happiness;

            // Starting energy is 20, so -40 leaves a 20 point shortfall.
            _resources.ApplyTurn(new ResourceVector { energy = -40f });

            Assert.AreEqual(0f, _resources.Current.energy, 0.001f, "blackout floors energy at zero");
            Assert.AreEqual(startingHappiness - 10f, _resources.Current.happiness, 0.001f,
                "shortfall should cost half its size in happiness");
        }

        [Test]
        public void SustainabilityIsClampedToItsBudget()
        {
            _resources.ApplyTurn(new ResourceVector { sustainability = 500f });
            Assert.AreEqual(ResourceManager.SustainabilityMax, _resources.Current.sustainability, 0.001f);

            _resources.ApplyTurn(new ResourceVector { sustainability = -99999f });
            Assert.AreEqual(0f, _resources.Current.sustainability, 0.001f);
        }

        [Test]
        public void ZeroSustainabilityEndsTheRun()
        {
            bool victory = true;
            _resources.OnGameEnded += won => victory = won;

            _resources.ApplyTurn(new ResourceVector { sustainability = -99999f });

            Assert.IsTrue(_resources.GameOver);
            Assert.IsFalse(victory, "running out of sustainability is a defeat");
        }

        [Test]
        public void TurnsAfterGameOverAreIgnored()
        {
            _resources.ApplyTurn(new ResourceVector { sustainability = -99999f });
            float moneyAtEnd = _resources.Current.money;

            _resources.ApplyTurn(new ResourceVector { money = 500f });

            Assert.AreEqual(moneyAtEnd, _resources.Current.money, 0.001f);
        }

        [Test]
        public void TrySpendRefusesWhenItCannotAfford()
        {
            Assert.IsFalse(_resources.TrySpend(_resources.Current.money + 1f));
            Assert.IsTrue(_resources.TrySpend(10f));
        }

        [Test]
        public void ApplyInstantSkipsTheBlackoutPenalty()
        {
            float startingHappiness = _resources.Current.happiness;

            // A build bonus is not a year passing, so it must not trigger blackout.
            _resources.ApplyInstant(new ResourceVector { energy = -9999f });

            Assert.AreEqual(0f, _resources.Current.energy, 0.001f);
            Assert.AreEqual(startingHappiness, _resources.Current.happiness, 0.001f);
            Assert.IsFalse(_resources.GameOver);
        }
    }
}
