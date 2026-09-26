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
    dict(key="BlacksmithShop",  builder="build_blacksmith_shop",  tri_budget=400, subdir="Castle"),
    dict(key="BarracksBunk",    builder="build_barracks_bunk",    tri_budget=450, subdir="Castle"),
    dict(key="WellCourtyard",   builder="build_well_courtyard",   tri_budget=500, subdir="Castle"),
    dict(key="StorehouseRoom",  builder="build_storehouse_room",  tri_budget=500, subdir="Castle"),
    # InnerWard
    dict(key="GreatHallMain",       builder="build_great_hall_main",       tri_budget=600, subdir="Castle"),
    dict(key="ChapelRoom",          builder="build_chapel_room",           tri_budget=400, subdir="Castle"),
    dict(key="KitchenRoom",         builder="build_kitchen_room",          tri_budget=500, subdir="Castle"),
    dict(key="GuardRoomInner",      builder="build_guard_room_inner",      tri_budget=400, subdir="Castle"),
    dict(key="ArmouredCourtyard",   builder="build_armoured_courtyard",    tri_budget=450, subdir="Castle"),
    # Keep
    dict(key="ThroneRoomKeep",    builder="build_throne_room_keep",    tri_budget=450, subdir="Castle"),
    dict(key="TreasuryVault",     builder="build_treasury_vault",      tri_budget=850, subdir="Castle"),
    dict(key="RoyalBedchamber",   builder="build_royal_bedchamber",    tri_budget=400, subdir="Castle"),
    dict(key="LordsSolar",        builder="build_lords_solar",         tri_budget=400, subdir="Castle"),
    dict(key="KeepStairwell",     builder="build_keep_stairwell",      tri_budget=600, subdir="Castle"),
    # Crypt
    dict(key="CryptAntechamber",  builder="build_crypt_antechamber",  tri_budget=350, subdir="Castle"),
    dict(key="TombCorridor",      builder="build_tomb_corridor",      tri_budget=450, subdir="Castle"),
    dict(key="BurialVault",       builder="build_burial_vault",       tri_budget=350, subdir="Castle"),
    dict(key="CryptChamberFinal", builder="build_crypt_chamber_final",tri_budget=800, subdir="Castle"),
    dict(key="CryptStairwell",    builder="build_crypt_stairwell",    tri_budget=600, subdir="Castle"),
]

ALL_SPECS = WEAPON_SPECS + LOOT_SPECS + CASTLE_SPECS
