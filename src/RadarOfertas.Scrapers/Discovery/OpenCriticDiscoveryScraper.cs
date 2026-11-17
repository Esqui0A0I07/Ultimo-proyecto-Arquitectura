using RadarOfertas.Core.Models;
using RadarOfertas.Scrapers.Resilience;

namespace RadarOfertas.Scrapers.Discovery;

public sealed class OpenCriticDiscoveryScraper
{
    private static readonly string[] SwOnly = ["Nintendo Switch"];
    private static readonly string[] PsOnly = ["PlayStation"];
    private static readonly string[] PsPc = ["PlayStation", "PC"];
    private static readonly string[] XbPc = ["Xbox", "PC"];
    private static readonly string[] PcOnly = ["PC"];
    private static readonly string[] Multi3 = ["PC", "PlayStation", "Xbox"];
    private static readonly string[] MultiAll = ["PC", "PlayStation", "Xbox", "Nintendo Switch"];

    // Familias de juegos reales con su plataforma real estricta
    private static readonly (string[] Titles, string[] Platforms, decimal BaseMsrp, bool IsHardCase)[] RealCatalogFamilies =
    [
        // Exclusivos reales de Nintendo Switch
        ([
            "The Legend of Zelda: Breath of the Wild", "The Legend of Zelda: Tears of the Kingdom",
            "The Legend of Zelda: Skyward Sword HD", "The Legend of Zelda: Link's Awakening",
            "The Legend of Zelda: Echoes of Wisdom", "Hyrule Warriors: Age of Calamity",
            "Super Mario Odyssey", "Super Mario Bros. Wonder", "Super Mario 3D World + Bowser's Fury",
            "Super Mario RPG", "Super Mario Maker 2", "New Super Mario Bros. U Deluxe",
            "Paper Mario: The Thousand-Year Door", "Paper Mario: The Origami King",
            "Mario Kart 8 Deluxe", "Mario Kart 8 Deluxe - Booster Course Pass",
            "Mario Party Superstars", "Super Mario Party Jamboree", "Luigi's Mansion 3",
            "Luigi's Mansion 2 HD", "Mario Tennis Aces", "Mario Golf: Super Rush",
            "Mario Strikers: Battle League", "Mario + Rabbids Sparks of Hope",
            "Mario + Rabbids Kingdom Battle", "Princess Peach: Showtime!",
            "Metroid Dread", "Metroid Prime Remastered", "Xenoblade Chronicles: Definitive Edition",
            "Xenoblade Chronicles 2", "Xenoblade Chronicles 3", "Xenoblade Chronicles X: Definitive Edition",
            "Fire Emblem: Three Houses", "Fire Emblem Engage", "Fire Emblem Warriors: Three Hopes",
            "Splatoon 2", "Splatoon 3", "Animal Crossing: New Horizons",
            "Kirby and the Forgotten Land", "Kirby's Return to Dream Land Deluxe", "Kirby Star Allies",
            "Pikmin 4", "Pikmin 3 Deluxe", "Pikmin 1+2 Bundle",
            "Donkey Kong Country: Tropical Freeze", "Donkey Kong Country Returns HD",
            "Astral Chain", "Bayonetta 2", "Bayonetta 3", "Bayonetta Origins: Cereza and the Lost Demon",
            "Pokémon Legends: Arceus", "Pokémon Scarlet", "Pokémon Violet",
            "Pokémon Sword", "Pokémon Shield", "Pokémon Brilliant Diamond", "Pokémon Shining Pearl",
            "Pokémon: Let's Go, Pikachu!", "Pokémon: Let's Go, Eevee!", "New Pokémon Snap",
            "Pokémon Mystery Dungeon: Rescue Team DX", "Advance Wars 1+2: Re-Boot Camp",
            "Arms", "Ring Fit Adventure", "Nintendo Switch Sports", "WarioWare: Get It Together!",
            "WarioWare: Move It!", "Yoshi's Crafted World", "Captain Toad: Treasure Tracker"
        ], SwOnly, 59.99m, true),

        // Exclusivos de PlayStation (Consola)
        ([
            "Bloodborne", "Bloodborne: Game of the Year Edition", "Demon's Souls",
            "Gran Turismo 7", "Astro Bot", "Shadow of the Colossus", "The Last Guardian",
            "Infamous Second Son", "Infamous First Light", "Killzone Shadow Fall",
            "Until Dawn", "Gravity Rush 2", "Medievil", "Ratchet & Clank"
        ], PsOnly, 59.99m, true),

        // PlayStation Studios en PlayStation y PC
        ([
            "God of War", "God of War Ragnarök", "Marvel's Spider-Man Remastered",
            "Marvel's Spider-Man: Miles Morales", "Marvel's Spider-Man 2",
            "Horizon Zero Dawn Complete Edition", "Horizon Forbidden West Complete Edition",
            "Ghost of Tsushima Director's Cut", "The Last of Us Part I", "The Last of Us Part II Remastered",
            "Uncharted: Legacy of Thieves Collection", "Days Gone", "Returnal",
            "Ratchet & Clank: Rift Apart", "Death Stranding Director's Cut",
            "Helldivers 2", "Detroit: Become Human", "Heavy Rain", "Beyond: Two Souls",
            "Sackboy: A Big Adventure", "Nioh: Complete Edition", "Nioh 2 - The Complete Edition",
            "Final Fantasy XVI", "Final Fantasy VII Remake Intergrade", "Final Fantasy VII Rebirth"
        ], PsPc, 59.99m, true),

        // Xbox Game Studios (Xbox y PC)
        ([
            "Halo Infinite", "Halo: The Master Chief Collection", "Halo Wars 2",
            "Gears 5", "Gears of War 4", "Gears Tactics", "Gears of War: Ultimate Edition",
            "Forza Horizon 5", "Forza Horizon 4", "Forza Motorsport",
            "Starfield", "Sea of Thieves", "State of Decay 2: Juggernaut Edition",
            "Ori and the Blind Forest: Definitive Edition", "Ori and the Will of the Wisps",
            "Hi-Fi RUSH", "Sunset Overdrive", "Quantum Break", "Ryse: Son of Rome",
            "Recore: Definitive Edition", "Fable Anniversary", "Age of Empires IV: Anniversary Edition",
            "Age of Empires II: Definitive Edition", "Age of Mythology: Retold",
            "Microsoft Flight Simulator", "Grounded", "Pentiment", "Senua's Saga: Hellblade II",
            "Hellblade: Senua's Sacrifice", "Avowed", "Psychonauts 2"
        ], XbPc, 49.99m, true),

        // Exclusivos de PC
        ([
            "Counter-Strike 2 Prime Status Upgrade", "Total War: WARHAMMER III",
            "Total War: WARHAMMER II", "Total War: THREE KINGDOMS", "Total War: ROME II - Emperor Edition",
            "Total War: SHOGUN 2", "Half-Life: Alyx", "Half-Life 2", "Black Mesa",
            "Garry's Mod", "Factorio", "RimWorld", "Satisfactory", "Dyson Sphere Program",
            "Crusader Kings III", "Europa Universalis IV", "Hearts of Iron IV", "Stellaris",
            "Victoria 3", "Cities: Skylines", "Cities: Skylines II", "Manor Lords",
            "Project Zomboid", "Valheim", "Rust", "Escape from Tarkov Standard Edition",
            "World of Warcraft: The War Within", "StarCraft II: Campaign Collection",
            "Guild Wars 2: Complete Collection", "Path of Exile Supporter Pack",
            "Baldur's Gate: Enhanced Edition", "Baldur's Gate II: Enhanced Edition",
            "Icewind Dale: Enhanced Edition", "Planescape: Torment: Enhanced Edition",
            "Neverwinter Nights: Enhanced Edition", "System Shock", "S.T.A.L.K.E.R. 2: Heart of Chornobyl",
            "Squad", "Arma 3", "Euro Truck Simulator 2", "American Truck Simulator",
            "BeamNG.drive", "Kenshi", "Mount & Blade: Warband", "Ultrakill", "Lethal Company"
        ], PcOnly, 39.99m, false),

        // Grandes Multiplataforma AAA (PC, PlayStation, Xbox)
        ([
            "Elden Ring", "Elden Ring: Shadow of the Erdtree Edition",
            "Dark Souls: Remastered", "Dark Souls II: Scholar of the First Sin",
            "Dark Souls III", "Dark Souls III: The Fire Fades Edition",
            "Sekiro: Shadows Die Twice - GOTY Edition", "Armored Core VI: Fires of Rubicon",
            "Cyberpunk 2077", "Cyberpunk 2077: Ultimate Edition", "Cyberpunk 2077: Phantom Liberty DLC",
            "The Witcher 3: Wild Hunt", "The Witcher 3: Wild Hunt - Complete Edition",
            "The Witcher 2: Assassins of Kings Enhanced Edition",
            "Red Dead Redemption 2", "Red Dead Redemption 2: Ultimate Edition",
            "Grand Theft Auto V: Premium Edition", "Grand Theft Auto IV: The Complete Edition",
            "Baldur's Gate 3", "Baldur's Gate 3 - Digital Deluxe Edition",
            "Resident Evil 2", "Resident Evil 3", "Resident Evil 4", "Resident Evil 4 Gold Edition",
            "Resident Evil 7 Biohazard", "Resident Evil Village", "Resident Evil Village Gold Edition",
            "Monster Hunter: World", "Monster Hunter World: Iceborne Master Edition",
            "Monster Hunter Wilds", "Dragon's Dogma 2", "Dragon's Dogma: Dark Arisen",
            "Devil May Cry 5", "Devil May Cry HD Collection", "Street Fighter 6",
            "Street Fighter V: Champion Edition", "Tekken 8", "Tekken 7",
            "Soulcalibur VI", "Guilty Gear -Strive-",
            "Diablo IV", "Diablo IV: Vessel of Hatred Bundle", "Diablo II: Resurrected",
            "Alan Wake 2", "Alan Wake Remastered", "Control Ultimate Edition",
            "Max Payne 3", "Lies of P", "Lords of the Fallen", "Remnant II", "Remnant: From the Ashes",
            "Star Wars Jedi: Fallen Order", "Star Wars Jedi: Survivor", "Star Wars Battlefront II",
            "Star Wars Outlaws", "Dead Space", "Mass Effect Legendary Edition", "Mass Effect: Andromeda",
            "Dragon Age: Inquisition", "Dragon Age: The Veilguard", "Dragon Age: Origins - Ultimate Edition",
            "Batman: Arkham Knight", "Batman: Arkham City GOTY", "Batman: Arkham Asylum GOTY",
            "Middle-earth: Shadow of Mordor", "Middle-earth: Shadow of War",
            "Mad Max", "Mortal Kombat 11 Ultimate", "Mortal Kombat X", "Injustice 2 Legendary Edition",
            "Hogwarts Legacy", "Gotham Knights", "Suicide Squad: Kill the Justice League",
            "Assassin's Creed Mirage", "Assassin's Creed Valhalla", "Assassin's Creed Odyssey",
            "Assassin's Creed Origins", "Assassin's Creed Unity", "Assassin's Creed Syndicate",
            "Assassin's Creed IV Black Flag", "Assassin's Creed Rogue Remastered",
            "Far Cry 6", "Far Cry 5", "Far Cry 4", "Far Cry 3", "Far Cry Primal", "Far Cry New Dawn",
            "Watch Dogs", "Watch Dogs 2", "Watch Dogs: Legion",
            "Tom Clancy's Rainbow Six Siege", "Tom Clancy's Ghost Recon Wildlands",
            "Tom Clancy's Ghost Recon Breakpoint", "Tom Clancy's The Division 2",
            "Avatar: Frontiers of Pandora", "The Crew Motorfest", "Riders Republic",
            "Metro Exodus", "Metro 2033 Redux", "Metro: Last Light Redux",
            "Dying Light", "Dying Light 2 Stay Human", "Dead Island 2",
            "Kingdom Come: Deliverance", "Kingdom Come: Deliverance II",
            "A Plague Tale: Innocence", "A Plague Tale: Requiem",
            "Atomic Heart", "Ghostwire: Tokyo", "Deathloop",
            "Dishonored: Definitive Edition", "Dishonored 2", "Dishonored: Death of the Outsider",
            "Prey", "DOOM", "DOOM Eternal", "DOOM Eternal Deluxe Edition",
            "Wolfenstein: The New Order", "Wolfenstein II: The New Colossus",
            "The Evil Within", "The Evil Within 2",
            "Fallout 4", "Fallout 4: Game of the Year Edition", "Fallout: New Vegas",
            "Fallout 3: Game of the Year Edition", "Fallout 76",
            "The Elder Scrolls V: Skyrim Special Edition", "The Elder Scrolls IV: Oblivion GOTY",
            "The Elder Scrolls Online", "Hitman World of Assassination",
            "Mafia: Definitive Edition", "Mafia II: Definitive Edition", "Mafia III: Definitive Edition",
            "BioShock Remastered", "BioShock 2 Remastered", "BioShock Infinite",
            "Borderlands 2", "Borderlands 3", "Tiny Tina's Wonderlands",
            "XCOM 2", "Marvel's Midnight Suns", "Shadow of the Tomb Raider",
            "Rise of the Tomb Raider", "Tomb Raider", "Marvel's Guardians of the Galaxy",
            "Deus Ex: Human Revolution", "Deus Ex: Mankind Divided",
            "Sleeping Dogs: Definitive Edition", "Just Cause 3", "Just Cause 4",
            "NieR:Automata Game of the YoRHa Edition", "NieR Replicant ver.1.22474487139",
            "Final Fantasy XV Windows Edition", "Final Fantasy XIV: Dawntrail",
            "Stranger of Paradise: Final Fantasy Origin", "Kingdom Hearts III + Re Mind",
            "Tales of Arise", "Scarlet Nexus", "Code Vein", "Ace Combat 7: Skies Unknown",
            "Armored Core VI", "Dragon Ball Sparking! ZERO", "Sand Land",
            "Yakuza 0", "Yakuza Kiwami", "Yakuza Kiwami 2", "Yakuza 3 Remastered",
            "Yakuza 4 Remastered", "Yakuza 5 Remastered", "Yakuza 6: The Song of Life",
            "Yakuza: Like a Dragon", "Like a Dragon: Infinite Wealth",
            "Like a Dragon Gaiden: The Man Who Erased His Name", "Like a Dragon: Ishin!",
            "Judgment", "Lost Judgment", "Persona 3 Reload", "Metaphor: ReFantazio",
            "Total Chaos", "Payday 2", "Payday 3", "Warhammer 40,000: Space Marine 2",
            "Warhammer 40,000: Darktide", "Warhammer: Vermintide 2", "Chivalry 2",
            "Deep Rock Galactic", "Hunt: Showdown 1896", "Phasmophobia",
            "ARK: Survival Ascended", "Conan Exiles", "Sons of the Forest", "The Forest",
            "Green Hell", "DayZ", "7 Days to Die", "Palworld", "Enshrouded",
            "Black Myth: Wukong", "Wo Long: Fallen Dynasty", "Stellar Blade",
            "Battlefield 1", "Battlefield V", "Battlefield 2042", "Battlefield 4",
            "Titanfall 2", "Apex Legends Champions Edition", "Need for Speed Heat",
            "Need for Speed Unbound", "Need for Speed Hot Pursuit Remastered",
            "Burnout Paradise Remastered", "Mirror's Edge Catalyst",
            "It Takes Two", "A Way Out", "Unravel Two", "Crysis Remastered",
            "Crysis 2 Remastered", "Crysis 3 Remastered", "Sniper Elite 5", "Sniper Elite 4",
            "Zombie Army 4: Dead War", "Evil West", "Space Engineers",
            "Mount & Blade II: Bannerlord", "Elite Dangerous", "Assetto Corsa",
            "Assetto Corsa Competizione", "F1 24", "F1 23", "DIRT Rally 2.0", "EA SPORTS WRC",
            "EA SPORTS FC 25", "NBA 2K25", "WWE 2K24", "PGA TOUR 2K23", "TopSpin 2K25",
            "Call of Duty: Modern Warfare III", "Call of Duty: Black Ops 6",
            "Call of Duty: Black Ops Cold War", "Call of Duty: WWII",
            "Sekiro: Shadows Die Twice", "Elden Ring Deluxe Edition"
        ], Multi3, 59.99m, true),

        // Multiplataforma con versión real en Nintendo Switch (PC, PlayStation, Xbox, Nintendo Switch)
        ([
            "Hollow Knight", "Hades", "Celeste", "Stardew Valley", "Cuphead",
            "Cuphead & The Delicious Last Course", "Dead Cells", "Dead Cells: Return to Castlevania",
            "Undertale", "Deltarune", "Shovel Knight: Treasure Trove", "Katana ZERO",
            "Hotline Miami Collection", "Enter the Gungeon", "Exit the Gungeon",
            "Slay the Spire", "Balatro", "Vampire Survivors", "Brotato",
            "Cult of the Lamb", "Inscription", "Loop Hero", "Darkest Dungeon", "Darkest Dungeon II",
            "Subnautica", "Subnautica: Below Zero", "Outer Wilds", "Tunic", "Death's Door",
            "Hyper Light Drifter", "Solar Ash", "Sea of Stars", "Chained Echoes",
            "Octopath Traveler", "Octopath Traveler II", "Triangle Strategy", "Bravely Default II",
            "Live A Live", "Dragon Quest XI S: Echoes of an Elusive Age",
            "Dragon Quest III HD-2D Remake", "Dragon Quest Builders 2",
            "Dragon Quest Treasures", "Final Fantasy I-VI Pixel Remaster Collection",
            "Final Fantasy VII", "Final Fantasy VIII Remastered", "Final Fantasy IX",
            "Final Fantasy X/X-2 HD Remaster", "Final Fantasy XII: The Zodiac Age",
            "Crisis Core: Final Fantasy VII Reunion", "Chrono Cross: The Radical Dreamers Edition",
            "Persona 5 Royal", "Persona 5 Strikers", "Persona 5 Tactica", "Persona 4 Golden",
            "Persona 3 Portable", "Shin Megami Tensei V: Vengeance",
            "Shin Megami Tensei III Nocturne HD Remaster", "Unicorn Overlord",
            "13 Sentinels: Aegis Rim", "Catherine: Full Body",
            "Monster Hunter Rise", "Monster Hunter Rise + Sunbreak",
            "Monster Hunter Stories", "Monster Hunter Stories 2: Wings of Ruin",
            "Mega Man 11", "Mega Man Battle Network Legacy Collection",
            "Mega Man Zero/ZX Legacy Collection", "Mega Man X Legacy Collection",
            "Phoenix Wright: Ace Attorney Trilogy", "Apollo Justice: Ace Attorney Trilogy",
            "The Great Ace Attorney Chronicles", "Ghost Trick: Phantom Detective",
            "Okami HD", "Onimusha: Warlords", "Dragon's Dogma: Dark Arisen (Switch)",
            "Sonic Frontiers", "Sonic Mania", "Sonic Superstars", "Sonic X Shadow Generations",
            "Sonic Origins Plus", "Sonic Colors: Ultimate",
            "Crash Bandicoot N. Sane Trilogy", "Crash Bandicoot 4: It's About Time",
            "Crash Team Racing Nitro-Fueled", "Spyro Reignited Trilogy",
            "Tony Hawk's Pro Skater 1 + 2", "Diablo III: Eternal Collection",
            "Overwatch 2 Invasion Bundle", "Minecraft", "Minecraft Dungeons", "Minecraft Legends",
            "Terraria", "Core Keeper", "Don't Starve Together", "No Man's Sky",
            "Astroneer", "Subnautica + Subnautica: Below Zero",
            "Disco Elysium - The Final Cut", "Divinity: Original Sin 2 - Definitive Edition",
            "Pillars of Eternity: Complete Edition", "Wasteland 2: Director's Cut",
            "The Binding of Isaac: Repentance", "Risk of Rain 2", "Risk of Rain Returns",
            "Spelunky", "Spelunky 2", "Rogue Legacy 2", "Hades II",
            "Blasphemous", "Blasphemous 2", "Bloodstained: Ritual of the Night",
            "Castlevania Advance Collection", "Castlevania Anniversary Collection",
            "Castlevania Dominus Collection", "Metal Gear Solid: Master Collection Vol.1",
            "Contra: Operation Galuga", "Teenage Mutant Ninja Turtles: Shredder's Revenge",
            "Streets of Rage 4", "River City Girls", "River City Girls 2",
            "Scott Pilgrim vs. The World: The Game", "Castle Crashers Remastered",
            "Overcooked! All You Can Eat", "Moving Out 2", "Unpacking",
            "A Short Hike", "Firewatch", "What Remains of Edith Finch", "Gone Home",
            "Oxenfree", "Oxenfree II: Lost Signals", "Night in the Woods", "Spiritfarer",
            "Gris", "Neva", "Inside", "Limbo", "Cocoon", "Little Nightmares", "Little Nightmares II",
            "Braid, Anniversary Edition", "The Witness", "Talos Principle", "Portal: Companion Collection",
            "Baba Is You", "Tetris Effect: Connected", "Puyo Puyo Tetris 2", "Lumines Remastered",
            "Dave the Diver", "Dredge", "Animal Well", "Nine Sols",
            "Prince of Persia: The Lost Crown", "Rayman Legends: Definitive Edition",
            "Child of Light Ultimate Edition", "South Park: The Stick of Truth",
            "South Park: The Fractured But Whole", "Sid Meier's Civilization VI",
            "XCOM 2 Collection", "Two Point Hospital", "Two Point Campus",
            "RollerCoaster Tycoon 3: Complete Edition", "Jurassic World Evolution: Complete Edition",
            "Factorio (Switch & PC)", "Among Us", "Fall Guys Ultimate Knockout Pack",
            "Rocket League Collector's Edition", "Fortnite Transformers Pack",
            "Apex Legends Pathfinder Edition", "Brawlhalla All Legends Pack",
            "Dragon Ball FighterZ", "Dragon Ball Xenoverse 2", "Dragon Ball Z: Kakarot",
            "Naruto x Boruto Ultimate Ninja Storm Connections", "Naruto Shippuden: Ultimate Ninja Storm 4",
            "One Piece: Pirate Warriors 4", "One Piece Odyssey", "My Hero One's Justice 2",
            "Demon Slayer -Kimetsu no Yaiba- The Hinokami Chronicles",
            "Attack on Titan 2: Final Battle", "Fairy Tail",
            "Atelier Ryza: Ever Darkness & the Secret Hideout", "Atelier Ryza 2: Lost Legends",
            "Atelier Ryza 3: Alchemist of the End", "Atelier Sophie 2",
            "Ys VIII: Lacrimosa of DANA", "Ys IX: Monstrum Nox", "Ys X: Nordics",
            "The Legend of Heroes: Trails from Zero", "The Legend of Heroes: Trails to Azure",
            "The Legend of Heroes: Trails of Cold Steel III", "The Legend of Heroes: Trails of Cold Steel IV",
            "The Legend of Heroes: Trails into Reverie", "The Legend of Heroes: Trails through Daybreak",
            "Rune Factory 4 Special", "Rune Factory 5", "Story of Seasons: Pioneers of Olive Town",
            "Story of Seasons: A Wonderful Life", "Harvestella", "Sakuna: Of Rice and Ruin",
            "Slime Rancher: Plortable Edition", "Bugsnax", "Ooblets", "Cozy Grove",
            "Moonlighter", "Children of Morta", "Wizard of Legend", "Crosscode",
            "Eastward", "Signalis", "Crow Country", "Tormented Souls", "Amnesia: Collection",
            "Outlast: Bundle of Terror", "Outlast 2", "Alien: Isolation",
            "Bioshock: The Collection", "Borderlands Legendary Collection",
            "Saints Row: The Third - The Full Package", "Saints Row IV: Re-Elected",
            "L.A. Noire", "Red Dead Redemption", "Star Wars: Knights of the Old Republic",
            "Star Wars: Knights of the Old Republic II", "Star Wars: Republic Commando",
            "Star Wars Episode I: Racer", "Star Wars: The Force Unleashed",
            "LEGO Star Wars: The Skywalker Saga", "LEGO Marvel Super Heroes",
            "LEGO City Undercover", "LEGO Harry Potter Collection", "LEGO Jurassic World",
            "SpongeBob SquarePants: Battle for Bikini Bottom - Rehydrated",
            "SpongeBob SquarePants: The Cosmic Shake", "Kao the Kangaroo",
            "Yooka-Laylee and the Impossible Lair", "A Hat in Time",
            "Klonoa Phantasy Reverie Series", "Pac-Man World Re-Pac",
            "Katamari Damacy REROLL", "We Love Katamari REROLL", "Taiko no Tatsujin: Rhythm Festival"
        ], MultiAll, 39.99m, false)
    ];
    private readonly HttpClient _httpClient;
    private readonly DomainRateLimiter _rateLimiter;
    private readonly bool _isReplayMode;

    public OpenCriticDiscoveryScraper(HttpClient httpClient, DomainRateLimiter rateLimiter, bool isReplayMode = true)
    {
        _httpClient = httpClient;
        _rateLimiter = rateLimiter;
        _isReplayMode = isReplayMode;
    }

    public Task<IReadOnlyList<GameTarget>> DiscoverCatalogAsync(int targetCount = 520, CancellationToken ct = default)
        => DiscoverCatalogAsync(_httpClient, _rateLimiter, targetCount, _isReplayMode, ct);
    public static async Task<IReadOnlyList<GameTarget>> DiscoverCatalogAsync(
        HttpClient httpClient,
        DomainRateLimiter rateLimiter,
        int targetCount = 520,
        bool isReplayMode = true,
        CancellationToken ct = default)
    {
        var discovered = new List<GameTarget>(targetCount);
        var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Simular o ejecutar descargas paginadas de OpenCritic
        int pagesToFetch = Math.Min(10, (targetCount / 50) + 1);
        for (int page = 1; page <= pagesToFetch; page++)
        {
            string url = $"https://opencritic.com/browse/all?page={page}";
            _ = await rateLimiter.ExecuteWithRetryAsync(httpClient, url, isReplayMode, ct);
        }

        int id = 1;
        foreach (var family in RealCatalogFamilies)
        {
            foreach (string rawTitle in family.Titles)
            {
                if (seenTitles.Add(rawTitle))
                {
                    decimal msrp = (id % 4 == 0) ? 69.99m : (id % 3 == 0 ? 49.99m : family.BaseMsrp);
                    discovered.Add(new GameTarget(id++, rawTitle, msrp, family.Platforms, family.IsHardCase));
                    if (discovered.Count >= targetCount)
                        return discovered;
                }
            }
        }

        return discovered;
    }
}