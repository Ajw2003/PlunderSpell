"""
The vertical-slice props and castle modules this pipeline is responsible
for — one entry per ScriptableObject/RoomId already sitting in
Assets/_Project/Data/{Inventory,Loot,Castle} with no mesh behind it yet.
`builder` names a function in builders.py (weapons/loot) or
castle_builders.py (castle).
"""

WEAPON_SPECS = [
    # key,              builder,               tri_budget, out subdir
    dict(key="BronzeSword",    builder="build_bronze_sword",    tri_budget=400, subdir="Weapons"),
    dict(key="Longsword",      builder="build_longsword",       tri_budget=450, subdir="Weapons"),
    dict(key="FlintlockPistol",builder="build_flintlock_pistol",tri_budget=600, subdir="Weapons"),
    dict(key="Matchlock",      builder="build_matchlock",       tri_budget=650, subdir="Weapons"),
    dict(key="PaviseShield",   builder="build_pavise_shield",   tri_budget=500, subdir="Weapons"),
    dict(key="PlateHelm",      builder="build_plate_helm",      tri_budget=550, subdir="Weapons"),
    dict(key="PowderGrenade",  builder="build_powder_grenade",  tri_budget=250, subdir="Weapons"),
    dict(key="RoundShield",    builder="build_round_shield",    tri_budget=450, subdir="Weapons"),
]

LOOT_SPECS = [
    dict(key="AncientRelic", builder="build_ancient_relic", tri_budget=450, subdir="Loot"),
    dict(key="CopperPot",    builder="build_copper_pot",    tri_budget=400, subdir="Loot"),
    dict(key="GoldenGoblet", builder="build_golden_goblet", tri_budget=350, subdir="Loot"),
    dict(key="HeavyChest",   builder="build_heavy_chest",   tri_budget=700, subdir="Loot"),
    # lathed rather than stacked discs (real dished well), which legitimately
    # costs more than the other small loot pieces
    dict(key="SilverPlate",  builder="build_silver_plate",  tri_budget=420, subdir="Loot"),
]

# One entry per RoomId in CastleRoomRegistry.asset — see castle_builders.py
# for the geometry and Tools/AssetPipeline/README.md for the pipeline
# itself. tri_budget is ~1.5-2x the actual count build_assets.py reports
# (same headroom convention as WEAPON_SPECS/LOOT_SPECS above), not a
# round guess.
CASTLE_SPECS = [
    # CurtainWall — the wall itself, not an enclosed room
    dict(key="GatehouseModule", builder="build_gatehouse_module", tri_budget=700, subdir="Castle"),
    dict(key="WallStraight",    builder="build_wall_straight",    tri_budget=550, subdir="Castle"),
    dict(key="WallCorner",      builder="build_wall_corner",      tri_budget=400, subdir="Castle"),
    dict(key="Bastion",         builder="build_bastion",          tri_budget=550, subdir="Castle"),
    dict(key="Drawbridge",      builder="build_drawbridge",       tri_budget=650, subdir="Castle"),
    # OuterBailey
    dict(key="StableBlock",     builder="build_stable_block",     tri_budget=400, subdir="Castle"),
    dict(key="BlacksmithShop",  builder="build_blacksmith_shop",  tri_budget=700, subdir="Castle"),
    dict(key="BarracksBunk",    builder="build_barracks_bunk",    tri_budget=1000, subdir="Castle"),
    dict(key="WellCourtyard",   builder="build_well_courtyard",   tri_budget=800, subdir="Castle"),
    dict(key="StorehouseRoom",  builder="build_storehouse_room",  tri_budget=950, subdir="Castle"),
    # InnerWard
    dict(key="GreatHallMain",       builder="build_great_hall_main",       tri_budget=1350, subdir="Castle"),
    dict(key="ChapelRoom",          builder="build_chapel_room",           tri_budget=900, subdir="Castle"),
    dict(key="KitchenRoom",         builder="build_kitchen_room",          tri_budget=850, subdir="Castle"),
    dict(key="GuardRoomInner",      builder="build_guard_room_inner",      tri_budget=850, subdir="Castle"),
    dict(key="ArmouredCourtyard",   builder="build_armoured_courtyard",    tri_budget=700, subdir="Castle"),
    # Keep
    dict(key="ThroneRoomKeep",    builder="build_throne_room_keep",    tri_budget=800, subdir="Castle"),
    dict(key="TreasuryVault",     builder="build_treasury_vault",      tri_budget=850, subdir="Castle"),
    dict(key="RoyalBedchamber",   builder="build_royal_bedchamber",    tri_budget=400, subdir="Castle"),
    dict(key="LordsSolar",        builder="build_lords_solar",         tri_budget=750, subdir="Castle"),
    dict(key="KeepStairwell",     builder="build_keep_stairwell",      tri_budget=600, subdir="Castle"),
    # Crypt
    dict(key="CryptAntechamber",  builder="build_crypt_antechamber",  tri_budget=600, subdir="Castle"),
    dict(key="TombCorridor",      builder="build_tomb_corridor",      tri_budget=900, subdir="Castle"),
    dict(key="BurialVault",       builder="build_burial_vault",       tri_budget=900, subdir="Castle"),
    dict(key="CryptChamberFinal", builder="build_crypt_chamber_final",tri_budget=800, subdir="Castle"),
    dict(key="CryptStairwell",    builder="build_crypt_stairwell",    tri_budget=600, subdir="Castle"),
    # Door plugs — one per enclosed zone, sized to that zone's archway.
    # Not RoomIds: the generator instantiates these to seal an archway that
    # ends up facing an empty cell (CastleRoomRegistry.DoorPlugs).
    dict(key="DoorPlugOuterBailey", builder="build_door_plug_outer_bailey", tri_budget=40, subdir="Castle"),
    dict(key="DoorPlugInnerWard",   builder="build_door_plug_inner_ward",   tri_budget=40, subdir="Castle"),
    dict(key="DoorPlugKeep",        builder="build_door_plug_keep",         tri_budget=40, subdir="Castle"),
    dict(key="DoorPlugCrypt",       builder="build_door_plug_crypt",        tri_budget=40, subdir="Castle"),
]

# ── The other three Ages (docs/plans/era-castle-rooms.md) ──────────────────
# Same zones, heights and archways as CASTLE_SPECS above (the High Medieval
# set), one builder file per Age. `kind` says how build_assets validates the
# piece: "wall" (curtain wall, no walkway to keep clear), "room" (enclosed,
# walkway enforced) or "plug" (a door plug). Keys carry the Age prefix
# because the manifest, previews, loot anchors and RoomIds are keyed by name.
# `module` names the builder file (one per Age and zone, so the zones can be
# built in parallel without touching each other's files; the door plugs sit
# in the Age's base file), `era` the HistoricalEra enum value.

BRONZE_CASTLE_SPECS = [
    # CurtainWall
    dict(key="BronzeLionGate", builder="build_bronze_lion_gate", tri_budget=2400, subdir="Castle/BronzeAge", module="castle_builders_bronze_curtain", era="BronzeAge", zone="CurtainWall", kind="wall"),
    dict(key="BronzeWallStraight", builder="build_bronze_wall_straight", tri_budget=1200, subdir="Castle/BronzeAge", module="castle_builders_bronze_curtain", era="BronzeAge", zone="CurtainWall", kind="wall"),
    dict(key="BronzeWallCorner", builder="build_bronze_wall_corner", tri_budget=2400, subdir="Castle/BronzeAge", module="castle_builders_bronze_curtain", era="BronzeAge", zone="CurtainWall", kind="wall"),
    dict(key="BronzeBastion", builder="build_bronze_bastion", tri_budget=2000, subdir="Castle/BronzeAge", module="castle_builders_bronze_curtain", era="BronzeAge", zone="CurtainWall", kind="wall"),
    dict(key="BronzeGateApproach", builder="build_bronze_gate_approach", tri_budget=2000, subdir="Castle/BronzeAge", module="castle_builders_bronze_curtain", era="BronzeAge", zone="CurtainWall", kind="wall"),
    # OuterBailey
    dict(key="BronzeChariotShed", builder="build_bronze_chariot_shed", tri_budget=1600, subdir="Castle/BronzeAge", module="castle_builders_bronze_bailey", era="BronzeAge", zone="OuterBailey", kind="room"),
    dict(key="BronzeFoundry", builder="build_bronze_foundry", tri_budget=1600, subdir="Castle/BronzeAge", module="castle_builders_bronze_bailey", era="BronzeAge", zone="OuterBailey", kind="room"),
    dict(key="BronzeLevyBarracks", builder="build_bronze_levy_barracks", tri_budget=1600, subdir="Castle/BronzeAge", module="castle_builders_bronze_bailey", era="BronzeAge", zone="OuterBailey", kind="room"),
    dict(key="BronzeCistern", builder="build_bronze_cistern", tri_budget=1600, subdir="Castle/BronzeAge", module="castle_builders_bronze_bailey", era="BronzeAge", zone="OuterBailey", kind="room"),
    dict(key="BronzeOilPress", builder="build_bronze_oil_press", tri_budget=3200, subdir="Castle/BronzeAge", module="castle_builders_bronze_bailey", era="BronzeAge", zone="OuterBailey", kind="room"),
    # InnerWard
    dict(key="BronzePithosMagazine", builder="build_bronze_pithos_magazine", tri_budget=4800, subdir="Castle/BronzeAge", module="castle_builders_bronze_ward", era="BronzeAge", zone="InnerWard", kind="room"),
    dict(key="BronzeFrescoCourt", builder="build_bronze_fresco_court", tri_budget=2000, subdir="Castle/BronzeAge", module="castle_builders_bronze_ward", era="BronzeAge", zone="InnerWard", kind="room"),
    dict(key="BronzeShrine", builder="build_bronze_shrine", tri_budget=1600, subdir="Castle/BronzeAge", module="castle_builders_bronze_ward", era="BronzeAge", zone="InnerWard", kind="room"),
    dict(key="BronzePalaceKitchen", builder="build_bronze_palace_kitchen", tri_budget=1600, subdir="Castle/BronzeAge", module="castle_builders_bronze_ward", era="BronzeAge", zone="InnerWard", kind="room"),
    dict(key="BronzeTabletArchive", builder="build_bronze_tablet_archive", tri_budget=2400, subdir="Castle/BronzeAge", module="castle_builders_bronze_ward", era="BronzeAge", zone="InnerWard", kind="room"),
    # Keep
    dict(key="BronzeMegaron", builder="build_bronze_megaron", tri_budget=2400, subdir="Castle/BronzeAge", module="castle_builders_bronze_keep", era="BronzeAge", zone="Keep", kind="room"),
    dict(key="BronzeTreasury", builder="build_bronze_treasury", tri_budget=2000, subdir="Castle/BronzeAge", module="castle_builders_bronze_keep", era="BronzeAge", zone="Keep", kind="room"),
    dict(key="BronzeQueensHall", builder="build_bronze_queens_hall", tri_budget=1800, subdir="Castle/BronzeAge", module="castle_builders_bronze_keep", era="BronzeAge", zone="Keep", kind="room"),
    dict(key="BronzeBathRoom", builder="build_bronze_bath_room", tri_budget=2000, subdir="Castle/BronzeAge", module="castle_builders_bronze_keep", era="BronzeAge", zone="Keep", kind="room"),
    dict(key="BronzeMegaronStair", builder="build_bronze_megaron_stair", tri_budget=1600, subdir="Castle/BronzeAge", module="castle_builders_bronze_keep", era="BronzeAge", zone="Keep", kind="room"),
    # Crypt
    dict(key="BronzeDromos", builder="build_bronze_dromos", tri_budget=1600, subdir="Castle/BronzeAge", module="castle_builders_bronze_crypt", era="BronzeAge", zone="Crypt", kind="room"),
    dict(key="BronzeGraveCircle", builder="build_bronze_grave_circle", tri_budget=1600, subdir="Castle/BronzeAge", module="castle_builders_bronze_crypt", era="BronzeAge", zone="Crypt", kind="room"),
    dict(key="BronzeLarnaxVault", builder="build_bronze_larnax_vault", tri_budget=1600, subdir="Castle/BronzeAge", module="castle_builders_bronze_crypt", era="BronzeAge", zone="Crypt", kind="room"),
    dict(key="BronzeTholos", builder="build_bronze_tholos", tri_budget=2400, subdir="Castle/BronzeAge", module="castle_builders_bronze_crypt", era="BronzeAge", zone="Crypt", kind="room"),
    dict(key="BronzeShaftStair", builder="build_bronze_shaft_stair", tri_budget=2000, subdir="Castle/BronzeAge", module="castle_builders_bronze_crypt", era="BronzeAge", zone="Crypt", kind="room"),
    # Door plugs, one per enclosed zone: the High Medieval size, this Age's stone.
    dict(key="BronzeDoorPlugOuterBailey", builder="build_bronze_door_plug_outer_bailey", tri_budget=40, subdir="Castle/BronzeAge", module="castle_builders_bronze", era="BronzeAge", zone="OuterBailey", kind="plug"),
    dict(key="BronzeDoorPlugInnerWard", builder="build_bronze_door_plug_inner_ward", tri_budget=40, subdir="Castle/BronzeAge", module="castle_builders_bronze", era="BronzeAge", zone="InnerWard", kind="plug"),
    dict(key="BronzeDoorPlugKeep", builder="build_bronze_door_plug_keep", tri_budget=40, subdir="Castle/BronzeAge", module="castle_builders_bronze", era="BronzeAge", zone="Keep", kind="plug"),
    dict(key="BronzeDoorPlugCrypt", builder="build_bronze_door_plug_crypt", tri_budget=40, subdir="Castle/BronzeAge", module="castle_builders_bronze", era="BronzeAge", zone="Crypt", kind="plug"),
]

LATE_CASTLE_SPECS = [
    # CurtainWall
    dict(key="LateBarbican", builder="build_late_barbican", tri_budget=1200, subdir="Castle/LateMedieval", module="castle_builders_late_curtain", era="LateMedieval", zone="CurtainWall", kind="wall"),
    dict(key="LateWallStraight", builder="build_late_wall_straight", tri_budget=1200, subdir="Castle/LateMedieval", module="castle_builders_late_curtain", era="LateMedieval", zone="CurtainWall", kind="wall"),
    dict(key="LateWallCorner", builder="build_late_wall_corner", tri_budget=1200, subdir="Castle/LateMedieval", module="castle_builders_late_curtain", era="LateMedieval", zone="CurtainWall", kind="wall"),
    dict(key="LateBastion", builder="build_late_bastion", tri_budget=1200, subdir="Castle/LateMedieval", module="castle_builders_late_curtain", era="LateMedieval", zone="CurtainWall", kind="wall"),
    dict(key="LateDrawbridge", builder="build_late_drawbridge", tri_budget=1200, subdir="Castle/LateMedieval", module="castle_builders_late_curtain", era="LateMedieval", zone="CurtainWall", kind="wall"),
    # OuterBailey
    dict(key="LateArtilleryYard", builder="build_late_artillery_yard", tri_budget=4000, subdir="Castle/LateMedieval", module="castle_builders_late_bailey", era="LateMedieval", zone="OuterBailey", kind="room"),
    dict(key="LateGunFoundry", builder="build_late_gun_foundry", tri_budget=2000, subdir="Castle/LateMedieval", module="castle_builders_late_bailey", era="LateMedieval", zone="OuterBailey", kind="room"),
    dict(key="LateHandgunnerBarracks", builder="build_late_handgunner_barracks", tri_budget=2400, subdir="Castle/LateMedieval", module="castle_builders_late_bailey", era="LateMedieval", zone="OuterBailey", kind="room"),
    dict(key="LateBrewhouse", builder="build_late_brewhouse", tri_budget=2400, subdir="Castle/LateMedieval", module="castle_builders_late_bailey", era="LateMedieval", zone="OuterBailey", kind="room"),
    dict(key="LateTreadwheelWell", builder="build_late_treadwheel_well", tri_budget=2400, subdir="Castle/LateMedieval", module="castle_builders_late_bailey", era="LateMedieval", zone="OuterBailey", kind="room"),
    # InnerWard
    dict(key="LateCountingHouse", builder="build_late_counting_house", tri_budget=2400, subdir="Castle/LateMedieval", module="castle_builders_late_ward", era="LateMedieval", zone="InnerWard", kind="room"),
    dict(key="LateArmouryHall", builder="build_late_armoury_hall", tri_budget=3200, subdir="Castle/LateMedieval", module="castle_builders_late_ward", era="LateMedieval", zone="InnerWard", kind="room"),
    dict(key="LateSpitKitchen", builder="build_late_spit_kitchen", tri_budget=2400, subdir="Castle/LateMedieval", module="castle_builders_late_ward", era="LateMedieval", zone="InnerWard", kind="room"),
    dict(key="LateChantryChapel", builder="build_late_chantry_chapel", tri_budget=2400, subdir="Castle/LateMedieval", module="castle_builders_late_ward", era="LateMedieval", zone="InnerWard", kind="room"),
    dict(key="LateLibrary", builder="build_late_library", tri_budget=2400, subdir="Castle/LateMedieval", module="castle_builders_late_ward", era="LateMedieval", zone="InnerWard", kind="room"),
    # Keep
    dict(key="LateGreatHall", builder="build_late_great_hall", tri_budget=2400, subdir="Castle/LateMedieval", module="castle_builders_late_keep", era="LateMedieval", zone="Keep", kind="room"),
    dict(key="LateJewelHouse", builder="build_late_jewel_house", tri_budget=2000, subdir="Castle/LateMedieval", module="castle_builders_late_keep", era="LateMedieval", zone="Keep", kind="room"),
    dict(key="LateStateBedchamber", builder="build_late_state_bedchamber", tri_budget=2000, subdir="Castle/LateMedieval", module="castle_builders_late_keep", era="LateMedieval", zone="Keep", kind="room"),
    dict(key="LateTapestrySolar", builder="build_late_tapestry_solar", tri_budget=2000, subdir="Castle/LateMedieval", module="castle_builders_late_keep", era="LateMedieval", zone="Keep", kind="room"),
    dict(key="LateTurretStair", builder="build_late_turret_stair", tri_budget=2000, subdir="Castle/LateMedieval", module="castle_builders_late_keep", era="LateMedieval", zone="Keep", kind="room"),
    # Crypt
    dict(key="LateUndercroft", builder="build_late_undercroft", tri_budget=2400, subdir="Castle/LateMedieval", module="castle_builders_late_crypt", era="LateMedieval", zone="Crypt", kind="room"),
    dict(key="LateOubliette", builder="build_late_oubliette", tri_budget=2400, subdir="Castle/LateMedieval", module="castle_builders_late_crypt", era="LateMedieval", zone="Crypt", kind="room"),
    dict(key="LateCharnelHouse", builder="build_late_charnel_house", tri_budget=5000, subdir="Castle/LateMedieval", module="castle_builders_late_crypt", era="LateMedieval", zone="Crypt", kind="room"),
    dict(key="LateEffigyCrypt", builder="build_late_effigy_crypt", tri_budget=1600, subdir="Castle/LateMedieval", module="castle_builders_late_crypt", era="LateMedieval", zone="Crypt", kind="room"),
    dict(key="LateUndercroftStair", builder="build_late_undercroft_stair", tri_budget=2400, subdir="Castle/LateMedieval", module="castle_builders_late_crypt", era="LateMedieval", zone="Crypt", kind="room"),
    # Door plugs, one per enclosed zone: the High Medieval size, this Age's stone.
    dict(key="LateDoorPlugOuterBailey", builder="build_late_door_plug_outer_bailey", tri_budget=40, subdir="Castle/LateMedieval", module="castle_builders_late", era="LateMedieval", zone="OuterBailey", kind="plug"),
    dict(key="LateDoorPlugInnerWard", builder="build_late_door_plug_inner_ward", tri_budget=40, subdir="Castle/LateMedieval", module="castle_builders_late", era="LateMedieval", zone="InnerWard", kind="plug"),
    dict(key="LateDoorPlugKeep", builder="build_late_door_plug_keep", tri_budget=40, subdir="Castle/LateMedieval", module="castle_builders_late", era="LateMedieval", zone="Keep", kind="plug"),
    dict(key="LateDoorPlugCrypt", builder="build_late_door_plug_crypt", tri_budget=40, subdir="Castle/LateMedieval", module="castle_builders_late", era="LateMedieval", zone="Crypt", kind="plug"),
]

POWDER_CASTLE_SPECS = [
    # CurtainWall
    dict(key="PowderRavelinGate", builder="build_powder_ravelin_gate", tri_budget=1200, subdir="Castle/AgeOfPowder", module="castle_builders_powder_curtain", era="AgeOfPowder", zone="CurtainWall", kind="wall"),
    dict(key="PowderRampart", builder="build_powder_rampart", tri_budget=1200, subdir="Castle/AgeOfPowder", module="castle_builders_powder_curtain", era="AgeOfPowder", zone="CurtainWall", kind="wall"),
    dict(key="PowderSalientCorner", builder="build_powder_salient_corner", tri_budget=1200, subdir="Castle/AgeOfPowder", module="castle_builders_powder_curtain", era="AgeOfPowder", zone="CurtainWall", kind="wall"),
    dict(key="PowderArrowheadBastion", builder="build_powder_arrowhead_bastion", tri_budget=1200, subdir="Castle/AgeOfPowder", module="castle_builders_powder_curtain", era="AgeOfPowder", zone="CurtainWall", kind="wall"),
    dict(key="PowderGabionBridge", builder="build_powder_gabion_bridge", tri_budget=1200, subdir="Castle/AgeOfPowder", module="castle_builders_powder_curtain", era="AgeOfPowder", zone="CurtainWall", kind="wall"),
    # OuterBailey
    dict(key="PowderCoachHouse", builder="build_powder_coach_house", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_bailey", era="AgeOfPowder", zone="OuterBailey", kind="room"),
    dict(key="PowderGunPark", builder="build_powder_gun_park", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_bailey", era="AgeOfPowder", zone="OuterBailey", kind="room"),
    dict(key="PowderMusketeerBarracks", builder="build_powder_musketeer_barracks", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_bailey", era="AgeOfPowder", zone="OuterBailey", kind="room"),
    dict(key="PowderCooperage", builder="build_powder_cooperage", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_bailey", era="AgeOfPowder", zone="OuterBailey", kind="room"),
    dict(key="PowderOrangery", builder="build_powder_orangery", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_bailey", era="AgeOfPowder", zone="OuterBailey", kind="room"),
    # InnerWard
    dict(key="PowderMagazine", builder="build_powder_magazine", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_ward", era="AgeOfPowder", zone="InnerWard", kind="room"),
    dict(key="PowderBallroom", builder="build_powder_ballroom", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_ward", era="AgeOfPowder", zone="InnerWard", kind="room"),
    dict(key="PowderBaroqueChapel", builder="build_powder_baroque_chapel", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_ward", era="AgeOfPowder", zone="InnerWard", kind="room"),
    dict(key="PowderCopperKitchen", builder="build_powder_copper_kitchen", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_ward", era="AgeOfPowder", zone="InnerWard", kind="room"),
    dict(key="PowderParterreCourt", builder="build_powder_parterre_court", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_ward", era="AgeOfPowder", zone="InnerWard", kind="room"),
    # Keep
    dict(key="PowderLongGallery", builder="build_powder_long_gallery", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_keep", era="AgeOfPowder", zone="Keep", kind="room"),
    dict(key="PowderKunstkammer", builder="build_powder_kunstkammer", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_keep", era="AgeOfPowder", zone="Keep", kind="room"),
    dict(key="PowderAudienceChamber", builder="build_powder_audience_chamber", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_keep", era="AgeOfPowder", zone="Keep", kind="room"),
    dict(key="PowderParadeBedchamber", builder="build_powder_parade_bedchamber", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_keep", era="AgeOfPowder", zone="Keep", kind="room"),
    dict(key="PowderSilverVault", builder="build_powder_silver_vault", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_keep", era="AgeOfPowder", zone="Keep", kind="room"),
    dict(key="PowderGrandStair", builder="build_powder_grand_stair", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_keep", era="AgeOfPowder", zone="Keep", kind="room"),
    # Crypt
    dict(key="PowderCasemate", builder="build_powder_casemate", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_crypt", era="AgeOfPowder", zone="Crypt", kind="room"),
    dict(key="PowderCountermine", builder="build_powder_countermine", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_crypt", era="AgeOfPowder", zone="Crypt", kind="room"),
    dict(key="PowderFamilyVault", builder="build_powder_family_vault", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_crypt", era="AgeOfPowder", zone="Crypt", kind="room"),
    dict(key="PowderImperialTomb", builder="build_powder_imperial_tomb", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_crypt", era="AgeOfPowder", zone="Crypt", kind="room"),
    dict(key="PowderCryptStair", builder="build_powder_crypt_stair", tri_budget=1600, subdir="Castle/AgeOfPowder", module="castle_builders_powder_crypt", era="AgeOfPowder", zone="Crypt", kind="room"),
    # Door plugs, one per enclosed zone: the High Medieval size, this Age's stone.
    dict(key="PowderDoorPlugOuterBailey", builder="build_powder_door_plug_outer_bailey", tri_budget=40, subdir="Castle/AgeOfPowder", module="castle_builders_powder", era="AgeOfPowder", zone="OuterBailey", kind="plug"),
    dict(key="PowderDoorPlugInnerWard", builder="build_powder_door_plug_inner_ward", tri_budget=40, subdir="Castle/AgeOfPowder", module="castle_builders_powder", era="AgeOfPowder", zone="InnerWard", kind="plug"),
    dict(key="PowderDoorPlugKeep", builder="build_powder_door_plug_keep", tri_budget=40, subdir="Castle/AgeOfPowder", module="castle_builders_powder", era="AgeOfPowder", zone="Keep", kind="plug"),
    dict(key="PowderDoorPlugCrypt", builder="build_powder_door_plug_crypt", tri_budget=40, subdir="Castle/AgeOfPowder", module="castle_builders_powder", era="AgeOfPowder", zone="Crypt", kind="plug"),
]

ERA_CASTLE_SPECS = BRONZE_CASTLE_SPECS + LATE_CASTLE_SPECS + POWDER_CASTLE_SPECS

ALL_SPECS = WEAPON_SPECS + LOOT_SPECS + CASTLE_SPECS + ERA_CASTLE_SPECS



def key_matches(key: str, only: str) -> bool:
    """The --only filter shared by build_assets, validate_asset and
    render_previews_only: `only` is comma-separated tokens; a token that is a
    whole key matches just that key, any other token is a key prefix."""
    all_keys = {s["key"] for s in ALL_SPECS}
    for token in only.split(","):
        if key == token or (token not in all_keys and key.startswith(token)):
            return True
    return False
