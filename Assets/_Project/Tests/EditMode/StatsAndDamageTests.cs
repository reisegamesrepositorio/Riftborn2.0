using NUnit.Framework;
using Riftborn.Characters.Stats;
using Riftborn.Damage;
using UnityEngine;
namespace Riftborn.Tests
{
    public sealed class StatsAndDamageTests
    {
        [Test]
        public void StatModifiersRecalculateFinalValue()
        {
            var go = new GameObject("stats");
            var stats = go.AddComponent<CharacterStatsController>();
            stats.SetBaseValue(CharacterStat.STR, 10f);
            stats.AddModifier(new StatModifier("flat", this, CharacterStat.STR, flatValue: 5f));
            stats.AddModifier(new StatModifier("add", this, CharacterStat.STR, additivePercent: 0.1f));
            Assert.AreEqual(16.5f, stats.GetFinalValue(CharacterStat.STR), 0.001f);
            Object.DestroyImmediate(go);
        }
        [Test]
        public void DamageCalculatorSupportsCoreTypes()
        {
            Assert.AreEqual(10f, DamageCalculator.Calculate(new DamageRequest { BaseValue = 10f, Type = DamageType.Physical }).FinalAmount);
            Assert.AreEqual(12f, DamageCalculator.Calculate(new DamageRequest { BaseValue = 12f, Type = DamageType.Magical }).FinalAmount);
            Assert.AreEqual(7f, DamageCalculator.Calculate(new DamageRequest { BaseValue = 7f, Type = DamageType.True }).FinalAmount);
        }
    }
}
