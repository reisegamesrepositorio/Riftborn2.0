# Riftborn Architecture Foundation

Riftborn uses one controller per complete gameplay domain. The player object is a composition point that keeps references and lets focused systems communicate without centralizing gameplay rules.

## Character composition

`CharacterContext` caches references to character controllers and exposes them to other systems. Controllers should use explicit references through the context instead of repeated scene searches.

Core controllers in this foundation:

- `CharacterStatsController`: base values, modifiers, dirty recalculation and stat change events.
- `HealthController`: current/max health, precomputed damage intake, healing, death and revive foundation.
- `ResourceController`: generic resource values for Mana, Energy, Rage, Focus and custom resources.
- `ActionStateController`: source-based blocks for movement, attacks, casting, items and interaction.
- `DamageCalculator`: centralized first-pass damage calculation for physical, magical and true damage.
- `StatusEffectController`: lifecycle, reapply, stacks, cleanse, duration and event dispatch for active effects.
- `CombatController`, `TargetingController`, `AbilityController`: structural foundations for attacks, targets and abilities.
- `InventoryController`, `EquipmentController`, `RuneController`: foundations for items, equipment stat modifiers and rune pages.
- `CharacterAnimationController`: bridge from gameplay commands to `Animator` only.

## Status effects

Specific effects own their formulas and behavior. The controller only manages lifecycle and collection state.

Initial effects: Bleed, Burn, Poison, Stun, Root, Silence, Sleep, Slow, Shield and Regeneration.

## Git and LFS

Generated Unity folders and IDE caches are ignored. Assets, Packages, ProjectSettings and Unity `.meta` files remain versioned. Git LFS tracks large binary art, audio, video, font and archive formats; scripts, scenes, prefabs, materials, ScriptableObjects and `.meta` files stay in normal Git history.
