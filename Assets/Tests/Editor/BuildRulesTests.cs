using NUnit.Framework;
using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Managers;
using Solarpunk.Tiles;
using UnityEditor;
using UnityEngine;

namespace Solarpunk.Tests
{
    /// <summary>Covers the placement rules from design doc section 2.</summary>
    public class BuildRulesTests
    {
        private GameObject _host;
        private ResourceManager _resources;
        private BuildController _build;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("TestHost");
            _resources = _host.AddComponent<ResourceManager>();
            _resources.Initialize();

            _build = _host.AddComponent<BuildController>();
            var so = new SerializedObject(_build);
            so.FindProperty("resourceManager").objectReferenceValue = _resources;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        private static HexCell MakeCell(TerrainRelief relief)
        {
            var go = new GameObject("Hex");
            var cell = go.AddComponent<HexCell>();
            cell.coordinates = new HexCoordinates(0, 0);
            cell.relief = relief;
            return cell;
        }

        private static TileDefinition MakeTile(string name, TerrainRelief required, float cost,
            TileCategory category = TileCategory.PowerPlant)
        {
            var tile = ScriptableObject.CreateInstance<TileDefinition>();
            tile.displayName = name;
            tile.category = category;
            tile.requiredRelief = required;
            tile.buildCost = cost;
            return tile;
        }

        [Test]
        public void OpenGroundAcceptsAnUnrestrictedTile()
        {
            HexCell cell = MakeCell(TerrainRelief.Mutable);
            TileDefinition solar = MakeTile("Solar", TerrainRelief.Mutable, 250f);

            Assert.IsNull(_build.BlockReason(cell, solar));

            Object.DestroyImmediate(cell.gameObject);
        }

        [Test]
        public void ReliefRestrictedTileIsRefusedOnTheWrongTerrain()
        {
            HexCell open = MakeCell(TerrainRelief.Mutable);
            TileDefinition hydro = MakeTile("Hidreletrica", TerrainRelief.Waterfall, 800f);

            Assert.IsNotNull(_build.BlockReason(open, hydro), "hydro must not be buildable on open ground");

            HexCell waterfall = MakeCell(TerrainRelief.Waterfall);
            Assert.IsNull(_build.BlockReason(waterfall, hydro), "hydro belongs on a waterfall");

            Object.DestroyImmediate(open.gameObject);
            Object.DestroyImmediate(waterfall.gameObject);
        }

        [Test]
        public void TileIsRefusedWhenItCannotBeAfforded()
        {
            HexCell cell = MakeCell(TerrainRelief.Mutable);
            TileDefinition nuclear = MakeTile("Nuclear", TerrainRelief.Mutable,
                _resources.Current.money + 1f);

            Assert.IsNotNull(_build.BlockReason(cell, nuclear));

            Object.DestroyImmediate(cell.gameObject);
        }

        [Test]
        public void OccupiedHexIsRefused()
        {
            HexCell cell = MakeCell(TerrainRelief.Mutable);
            TileDefinition solar = MakeTile("Solar", TerrainRelief.Mutable, 250f);
            cell.builtTile = solar;

            Assert.IsNotNull(_build.BlockReason(cell, solar));

            Object.DestroyImmediate(cell.gameObject);
        }

        [Test]
        public void BuildingChargesTheCostAndAppliesTheFoundingBonus()
        {
            HexCell cell = MakeCell(TerrainRelief.Mutable);
            TileDefinition city = MakeTile("City", TerrainRelief.Mutable, 200f, TileCategory.City);
            city.onBuildBonus = new ResourceVector { population = 7f };

            float moneyBefore = _resources.Current.money;
            float popBefore = _resources.Current.population;

            Assert.IsTrue(_build.TryBuild(cell, city));
            Assert.AreEqual(moneyBefore - 200f, _resources.Current.money, 0.001f);
            Assert.AreEqual(popBefore + 7f, _resources.Current.population, 0.001f);

            Object.DestroyImmediate(cell.gameObject);
        }

        [Test]
        public void OnlyOneCityMayBeFounded()
        {
            HexCell first = MakeCell(TerrainRelief.Mutable);
            HexCell second = MakeCell(TerrainRelief.Mutable);
            TileDefinition city = MakeTile("City", TerrainRelief.Mutable, 200f, TileCategory.City);

            Assert.IsTrue(_build.TryBuild(first, city));
            Assert.IsNotNull(_build.BlockReason(second, city), "a second city must be refused");

            Object.DestroyImmediate(first.gameObject);
            Object.DestroyImmediate(second.gameObject);
        }
    }
}
