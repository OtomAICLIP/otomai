using Bubble.DamageCalculation;
using Bubble.DamageCalculation.SpellManagement;
using BubbleBot.Cli.Services.Maps;

namespace BubbleBot.Cli.Services.Fight;

public class AiEnvironment(FightActor fighter)
{
	public readonly FightActor Fighter = fighter;

	private static readonly SpellId[] UnauthorizedSpells = [
		SpellId.InvocationDeChaferfu,
        // Pandawa
        SpellId.Karcham, 
        SpellId.Chamrak,
        SpellId.Ebriete,
        SpellId.Ivresse,
        // Xelor
        SpellId.Gelure,
        SpellId.Rembobinage, 
        SpellId.ParadoxeTemporel,
        SpellId.Fuite,
        SpellId.Desynchronisation,
        SpellId.VingtCinquiemeHeure,
        SpellId.InstabiliteTemporelle,
        SpellId.RetourSpontane,
        //Cra
        SpellId.FlecheDeDispersion,
        SpellId.FlecheEmpoisonnee,
        SpellId.FlecheCinglante,
        SpellId.MiroirAuxAlouettes,
        SpellId.Vendetta,
        SpellId.OEilPourOEil,
        SpellId.Sentinelle,
        SpellId.Represailles,
        SpellId.AcuiteAbsolue,
        SpellId.BaliseDeRappel,
        SpellId.FlecheDeRepli,
        // Osamodas
        SpellId.Prime,
        SpellId.Favoritisme,
        SpellId.BaumeProtecteur,
        SpellId.Ponction,
        SpellId.Second,
        SpellId.PiqûreMotivante,
        SpellId.Tierce,
        SpellId.TandemAnimal,
        SpellId.Fouet,
        SpellId.Rappel,
        SpellId.Quart,
        SpellId.EquilibreBestial,
        SpellId.ResistanceNaturelle,
        SpellId.Quint,
        SpellId.Remplacement,
        SpellId.Relais,
        SpellId.PreserveNaturelle,
        SpellId.Cravache,
        SpellId.Sixte,
        // Enutrof
        SpellId.ClefDuTresor,
        SpellId.PelleAnimee,
        SpellId.Cupidite,
        SpellId.Vivacite,
        SpellId.RetraiteAnticipee,
        SpellId.Corruption,
        SpellId.MusetteAnimee,
        SpellId.Deambulation,
        SpellId.BoiteAOutils,
        SpellId.ClefDeBras,
        SpellId.BecheAnimee,
        SpellId.Avarice,
        SpellId.DernierRecours,
        SpellId.PelleDeFortune,
        SpellId.MalleAnimee,
        // Sram
        SpellId.Invisibilite,
        SpellId.Double,
        SpellId.Arsenic,
        SpellId.PiegeSournois,
        SpellId.PiegeFangeux,
        SpellId.PiegeFuneste,
        SpellId.PiegeRepulsif,
        SpellId.PiegeDImmobilisation,
        SpellId.Peur,
        SpellId.PiegeDeDerive,
        SpellId.PiegeScelerat,
        SpellId.ConcentrationDeChakra,
        SpellId.PiegeMortel,
        SpellId.Derobade,
        SpellId.Brume,
        SpellId.Comploteur,
        SpellId.PiegeDeMasse,
        SpellId.FosseCommune,
        SpellId.Meprise,
        SpellId.PiegeInsidieux,
        SpellId.Manigance,
        SpellId.MarqueMortuaire,
        // Ecaflip
        SpellId.ChanceDEcaflip,
        SpellId.BondDuFelin,
        SpellId.GriffeInvocatrice,
        SpellId.Aubaine,
        SpellId.Perception,
        SpellId.Roulette,
        SpellId.Contrecoup,
        SpellId.RoueDeLaFortune,
        SpellId.Trefle,
        SpellId.Odorat,
        SpellId.Entrechat,
        SpellId.CaresseInvocatrice,
        SpellId.Rugissement,
        SpellId.CoupDuSort,
        SpellId.Corollaire,
        SpellId.Mistigri,
        SpellId.BonneEtoile,
        SpellId.Redistribution,
        // Eniripsa = 7,
		SpellId.MotDAmitie,
		SpellId.MotDeFrayeur,
		SpellId.MotVivifiant,
		SpellId.MotDeJouvence,
		SpellId.MotAccablant,
		SpellId.MotDEnvol,
		SpellId.MotDeReconstitution,
		SpellId.MotAlchimique,
		SpellId.MotDeDeclin,
		SpellId.Scalpel,
		SpellId.MotGalvanisant,
		SpellId.MotDeprimant,
		SpellId.MotDeprimant,
		SpellId.MotDecourageant,
		SpellId.FontaineDeJouvence,
		SpellId.MotDeSolidarite,
		// Iop = 8
		SpellId.Rassemblement,
		SpellId.Friction,
		SpellId.Duel,
		SpellId.Precipitation,
		SpellId.Agitation,
		SpellId.Conquete,
		SpellId.Violence,
		SpellId.Vertu,
		SpellId.CoupPourCoup,
		SpellId.Emprise,
		SpellId.Vitalite,
		SpellId.Vindicte,
		SpellId.Determination,
		SpellId.Massacre,
		// Sadida = 10,
		SpellId.Arbre,
		SpellId.Folle,
		SpellId.Bloqueuse,
		SpellId.PoisonParalysant,
		SpellId.RonceApaisante,
		SpellId.PuissanceSylvestre,
		SpellId.Sacrifiee,
		SpellId.DonNaturel,
		SpellId.VentEmpoisonne,
		SpellId.Gonflable,
		SpellId.ArbreDeVie,
		SpellId.RonceInsolente,
		SpellId.Surpuissante,
		SpellId.ArbreFeuillu,
		SpellId.FolleTransmutee,
		SpellId.BloqueuseTransmutee,
		SpellId.Rempotage,
		SpellId.InfluenceVegetale,
		SpellId.SacrifieeTransmutee,
		SpellId.AltruismeVegetal,
		SpellId.GonflableTransmutee,
		SpellId.Harmonie,
		SpellId.SurpuissanteTransmutee,
		// Sacrieur = 11
		SpellId.Mutilation,
		SpellId.Transfusion,
		SpellId.Sacrifice,
		SpellId.Perfusion,
		SpellId.Fluctuation,
		SpellId.Pilori,
		SpellId.Riposte,
		SpellId.Penitence,
		// Roublard = 15
		SpellId.Detonateur,
		SpellId.Botte,
		SpellId.Aimantation,
		SpellId.Roublabot,
		SpellId.Roublardise,
		SpellId.Remission,
		SpellId.Poudre,
		SpellId.DernierSouffle,
		SpellId.Kaboom,
		SpellId.Etoupille,
		SpellId.Ruse,
		SpellId.Croisement,
		SpellId.BombeAmbulante,
		SpellId.Megabombe,
		SpellId.Stratageme,
		SpellId.Casemate,
		SpellId.PiegeMagnetique,
		SpellId.Imposture,
		// Zobal
		SpellId.Reuche,
		SpellId.Fougue,
		SpellId.Debandade,
		SpellId.Transe,
		SpellId.Carnavalo,
		SpellId.Pivot,
		SpellId.Ginga,
		SpellId.Armadur,
		SpellId.Massacre,
		SpellId.Comedie,
		SpellId.Nevrose,
		SpellId.Diffraction,
		SpellId.Transfiguration,
		//Steamer 
		SpellId.Harponneuse,
		SpellId.ArmureDeSel,
		SpellId.Scaphandre,
		SpellId.Aspiration,
		SpellId.Tactirelle,
		SpellId.Secourisme,
		SpellId.Cuirasse,
		SpellId.BriseLÂme,
		SpellId.Piston,
		SpellId.Transition,
		SpellId.Assistance,
		SpellId.Blindage,
		SpellId.Foreuse,
		SpellId.Surtension,
		SpellId.Sauvetage,
		SpellId.Sonar,
		SpellId.Revetement,
		SpellId.Compas,
		SpellId.Recursivite,
		//Eliotrope = 16
		SpellId.Portail,
		SpellId.Neutral,
		SpellId.Etirement,
		SpellId.Distribution,
		SpellId.Stupeur,
		SpellId.Cicatrisation,
		SpellId.Odyssee,
		SpellId.PaumeCurative,
		SpellId.Entraide,
	    SpellId.PortailFlexible,
		SpellId.Interruption,
		SpellId.Contraction,
		SpellId.Diffusion,
		SpellId.Conjuration,
		SpellId.Exode,
		SpellId.Cabale,
		SpellId.Conflagration,
		SpellId.Coalition,
		// Huppermage = 17
		SpellId.Runification,
		SpellId.Polarite,
		SpellId.BouclierElementaire,
		SpellId.Contribution,
		SpellId.TraitementRunique,
		SpellId.Propagation,
		SpellId.Empreinte,
		SpellId.Tribut,
		SpellId.Manifestation,
		SpellId.CourantQuadramental,
		SpellId.RepulsionRunique,
		SpellId.GardienElementaire,
		SpellId.Creation,
		SpellId.PiegeElementaire,
		SpellId.CycleElementaire,
		// Ouginak = 18
		SpellId.Proie,
		SpellId.Convergence,
		SpellId.Lanceroquet,
		SpellId.Arcanin,
		SpellId.PelageProtecteur,
		SpellId.Apaisement,
		SpellId.Panique,
		SpellId.Aboiement,
		SpellId.Flair,
		SpellId.AppelDeLaMeute,
		SpellId.Gibier,
		SpellId.Pistage,
		SpellId.Gangrene,
		SpellId.Caninos,
		SpellId.Ferocite,
		SpellId.Affection,
		SpellId.Poursuite,
		SpellId.Rogne,
		SpellId.Acharnement,
		SpellId.NouvelleLune14321,
        
        SpellId.MaitriseDArme,
        SpellId.CaptureDame,
        SpellId.Doom,
        SpellId.Tuerie,
        (SpellId)30032
		/*
		Cra = 9,
		Sadida = 10,
		Sacrieur = 11,
		Pandawa = 12,
		Roublard = 13,
		Zobal = 14,
		Steamer = 15,
		Eliotrope = 16,
		Huppermage = 17,
		Ouginak = 18,
		Forgelance = 20,
		*/
    ];

    public required FightContext FightContext { get; set; }
    public required FighterTranslator HaxeFighter { get; set; }
    public required IList<FightActor> Allies { get; init; }
    public required IList<FightActor> Enemies { get; init; }
    public required IList<Mark> Portals { get; init; }
    public required IList<SpellWrapper> Spells { get; set; }
    public readonly IDictionary<string, PathingValue> PathCache = new Dictionary<string, PathingValue>();
    public IDictionary<int, Dictionary<string, SpellScore>> SpellsScoreOnCell { get; } = new Dictionary<int, Dictionary<string, SpellScore>>();
    public required bool CanMove { get; init; }
    public bool CastedAnyDamageSpell { get; set; }
    public bool CastedAnySpell { get; set; }

    public bool IsFleeMonster => false;
    public int ForceElementId { get; set; } = -1;
    public bool IsPlayer { get; set; } = true;
    public bool IsFightEnded { get; set; }

    public void RefreshContext()
    {
	    SpellsScoreOnCell.Clear();
	    HaxeFighter  = Fighter.FighterTranslator.Clone();
	    FightContext = Fighter.Fight!.GetFightContextSimulation(HaxeFighter);
    }

    public IEnumerable<SpellWrapper> GetSpells()
    {
	    var spells = Fighter.Spells;

	    return spells.Where(x =>
		                        !UnauthorizedSpells.Contains((SpellId)x.Spell.Id) && 
		                        CouldBeCastSpell(x));
    }

    public bool CouldBeCastSpell(SpellWrapper castEntity)
    {	  
	    var caster = castEntity.Caster;

	    if(caster.Fighter == null)
	    {
		    return false;
	    }

	    var apCost = castEntity.GetApCost();
	    
	    if (apCost > caster.Fighter.Stats.Ap.Total)
	    {
		    return false;
	    }
	    
	    if (castEntity.Id == 13124)
	    {
            
	    }
	    var cooldown = castEntity.GetCooldown();
	    if (cooldown < 0)
	    {
		    return false;
	    }

	    return true;
    }
}

public record PathingValue(MovementPath Path, int PathCost);
public class SpellScore
{
	public double Score { get; set; }
	public double DamageOnAllies { get; set; }
	public double DamageOnEnemies { get; set; }
}