namespace Plunderspell.Spells
{
    /// <summary>
    /// Canonical identifier for every spell (and misfire outcome) in Plunderspell.
    /// Values are explicit so the enum can be serialised over PurrNet and persisted
    /// in ScriptableObject assets without shifting if the list is reordered later.
    /// </summary>
    public enum SpellId
    {
        None = 0,

        // --- Primary spells (the 8 keybound casts) ---
        Ignis = 1,          // fire bolt
        Frango = 2,         // break / shatter
        Levo = 3,           // levitate object
        AurumVoco = 4,      // summon gold
        Tonitrus = 5,       // thunderclap
        Somnus = 6,         // sleep
        CadaverSurge = 7,   // raise corpse
        Porta = 8,          // open door / portal

        // --- Extended lexicon ---
        Aqua = 9,
        Terra = 10,
        Ventus = 11,
        Lux = 12,
        Tenebris = 13,
        Glacies = 14,
        Fulmen = 15,
        Sanguis = 16,
        Animus = 17,
        Fortis = 18,
        Velox = 19,
        Caecus = 20,
        Sanus = 21,
        Tempus = 22,
        Verto = 23,
        Crescere = 24,
        Calor = 25,
        Frigus = 26,
        Murus = 27,
        PortaMagna = 28,
        Scutum = 29,
        Hasta = 30,
        Arcus = 31,
        Flamma = 32,
        Unda = 33,
        Saxum = 34,
        Silva = 35,
        Noctis = 36,
        Dies = 37,
        Vita = 38,
        Mors = 39,
        Bellum = 40,
        Pax = 41,
        Aurum = 42,
        Argentum = 43,
        Ferrum = 44,
        IgnisMagna = 45,
        TonitrusMagna = 46,

        // --- Misfire outcomes (near-match failures) ---
        MisfireIgnis = 100,        // sets caster on fire
        MisFireFrango = 101,       // breaks a random inventory item
        MisfireLevo = 102,         // levitates the caster uncontrollably
        MisfireTonitrus = 103,     // deafens the caster
        MisFireSomnus = 104,       // puts the caster to sleep
        MisFireCadaverSurge = 105, // nearest corpse explodes
        MisfireAurumVoco = 106,    // gold scatters
        MisfirePorta = 107,        // opens a random wrong door
    }
}
