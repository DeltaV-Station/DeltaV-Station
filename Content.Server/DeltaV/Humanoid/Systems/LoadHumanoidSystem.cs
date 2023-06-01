﻿using Content.Server.CharacterAppearance.Components;
using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;

namespace Content.Server.DeltaV.Humanoid.Systems;

public sealed class LoadHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly IEntitySystemManager _entitySys = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoadHumanoidAppearanceComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(EntityUid uid, LoadHumanoidAppearanceComponent component, PlayerAttachedEvent args)
    {

        if (!TryComp<MindComponent>(uid, out var mindComponent) || mindComponent.Mind?.UserId == null)
        {
            return; // Character has no mind, so we cant import their preferences.
        }

        if (!TryComp(uid, out HumanoidAppearanceComponent? humanoid) || !string.IsNullOrEmpty(humanoid.Initial))
        {
            return; // There is already a profile present, if we continue we may delete something unintended
        }


        var mind = mindComponent.Mind;
        var character = (HumanoidCharacterProfile) _prefs.GetPreferences(mind.UserId.Value).SelectedCharacter;
        var coordinates = uid.IsValid()
            ? _entityManager.GetComponent<TransformComponent>(uid).Coordinates
            : _entitySys.GetEntitySystem<GameTicker>().GetObserverSpawnPoint();

        var entity = _entityManager.System<StationSpawningSystem>()
            .SpawnPlayerMob(coordinates: coordinates, profile: character, entity: null, job: null, station: null);

        RemComp<LoadHumanoidAppearanceComponent>(entity);
        mind.TransferTo(entity);
        _entityManager.QueueDeleteEntity(uid);
    }
}
