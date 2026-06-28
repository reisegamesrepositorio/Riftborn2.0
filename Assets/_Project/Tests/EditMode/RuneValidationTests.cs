using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Riftborn.Characters.Runes;
using UnityEngine;
namespace Riftborn.Tests
{
    public sealed class RuneValidationTests
    {
        [Test]
        public void RunePageAllowsOnlyOneChoicePerLevel()
        {
            var page = ScriptableObject.CreateInstance<RunePageData>();
            FieldInfo selectionsField = typeof(RunePageData).GetField("selections", BindingFlags.NonPublic | BindingFlags.Instance);
            selectionsField.SetValue(page, new List<RuneSelection> { new RuneSelection { level = 0, nodeIndex = 0, effectId = "a" }, new RuneSelection { level = 0, nodeIndex = 1, effectId = "b" } });
            Assert.IsFalse(RuneController.ValidatePage(page));
            Object.DestroyImmediate(page);
        }
    }
}
