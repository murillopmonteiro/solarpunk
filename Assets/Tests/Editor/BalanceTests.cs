using NUnit.Framework;
using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Managers;
using Solarpunk.Tiles;
using UnityEditor;
using UnityEngine;

namespace Solarpunk.Tests
{
    /// <summary>
    /// Guards the design's stated balance targets. These encode intent, not
    /// current behaviour: if a change moves the collapse year, one of these
    /// fails and the decision becomes explicit rather than silent.
    /// </summary>
    public class BalanceTests
    {
        private GameObject _host;

        [SetUp]
        public void SetUp() => _host = new GameObject("TestHost");

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_host);

        /// <summary>Private serialized fields are set the same way SceneBootstrapper sets them.</summary>
        private static void SetVector(Object target, string field, ResourceVector value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            Assert.NotNull(prop, $"{target.GetType().Name}.{field} does not exist");
            prop.FindPropertyRelative("energy").floatValue = value.energy;
            prop.FindPropertyRelative("money").floatValue = value.money;
            prop.FindPropertyRelative("sustainability").floatValue = value.sustainability;
            prop.FindPropertyRelative("population").floatValue = value.population;
            prop.FindPropertyRelative("happiness").floatValue = value.happiness;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private CityGrowth MakeCity(ResourceVector effect)
        {
            var growth = _host.AddComponent<CityGrowth>();
            SetVector(growth, "cityEffect", effect);
            return growth;
        }

        private static readonly ResourceVector CityL1 =
            new ResourceVector { energy = -8f, money = -2f, sustainability = -1f, population = 5f, happiness = 0f };

        private static readonly ResourceVector CoalExtraction =
            new ResourceVector { energy = 0f, money = 20f, sustainability = -1f };

        private static readonly ResourceVector CoalThermal =
            new ResourceVector { energy = 10f, money = -3f, sustainability = -3f };

        [Test]
        public void CityEffectDoesNotScaleWithLevel()
        {
            CityGrowth growth = MakeCity(CityL1);

            // Regression: this used to return cityEffect * level, which ended runs
            // on happiness around year 9 instead of sustainability at year 200.
            ResourceVector effect = growth.GetPerTurnEffect();

            Assert.AreEqual(-8f, effect.energy, 0.001f);
            Assert.AreEqual(-1f, effect.sustainability, 0.001f);
            Assert.AreEqual(5f, effect.population, 0.001f);
        }

        [Test]
        public void CoalTriadSumsToTheDesignedVector()
        {
            ResourceVector total = CityL1 + CoalExtraction + CoalThermal;

            Assert.AreEqual(2f, total.energy, 0.001f, "E");
            Assert.AreEqual(15f, total.money, 0.001f, "M");
            Assert.AreEqual(-5f, total.sustainability, 0.001f, "S");
            Assert.AreEqual(5f, total.population, 0.001f, "P");
            Assert.AreEqual(0f, total.happiness, 0.001f, "H");
        }

        [Test]
        public void CoalTriadExhaustsSustainabilityAtYear200()
        {
            var resources = _host.AddComponent<ResourceManager>();
            resources.Initialize();

            ResourceVector perYear = CityL1 + CoalExtraction + CoalThermal;

            int year = 0;
            while (!resources.GameOver && year < 400)
            {
                year++;
                resources.ApplyTurn(perYear);
            }

            Assert.AreEqual(200, year, "the coal triad should reach zero sustainability at year 200");
            Assert.AreEqual(0f, resources.Current.sustainability, 0.001f);
            Assert.Greater(resources.Current.happiness, 0f, "the run should end on sustainability, not happiness");
        }

        [Test]
        public void SustainabilityBudgetIsOneThousand()
        {
            // The triad figures are whole numbers per year; 1000 is what makes
            // 1000 / 5 = year 200 work. On a 100 budget it would be year 20.
            var resources = _host.AddComponent<ResourceManager>();
            resources.Initialize();

            Assert.AreEqual(ResourceManager.SustainabilityMax, resources.Current.sustainability, 0.001f);
            Assert.AreEqual(1000f, ResourceManager.SustainabilityMax, 0.001f);
        }
    }
}
