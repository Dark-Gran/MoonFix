using System;
using Partiality.Modloader;
using UnityEngine;

public class KarmaAppetite_MoonFix : PartialityMod
{

    public KarmaAppetite_MoonFix()
    {
        instance = this;
        this.ModID = "KarmaAppetite_MoonFix";
        this.Version = "0.1";
        this.author = "DarkGran";
    }

    public static KarmaAppetite_MoonFix instance;

    public override void OnEnable()
    {
        base.OnEnable();
		On.RainWorldGame.Win += RainWorldGame_Win;
		On.SLOrcacleState.InfluenceLike += SLOrcacleState_InfluenceLike;
        On.SLOrcacleState.FromString += SLOrcacleState_FromString;
	}

	//1. FIXING "MORE THAN LIKE YET NO LOVE BUG"

	public static void FixInfluenceCap(SLOrcacleState self)
	{
		//TURNS OUT LERPMAP FOR THE OPINION-ENUMS IS WRONG
		//Lets cap it at the last enum (which is 3, not 4)

		if ((int)self.GetOpinion == 4)
		{
			self.likesPlayer = 0.95f; //self.GetOpinion = 3
		}

	}

	private static void SLOrcacleState_InfluenceLike(On.SLOrcacleState.orig_InfluenceLike orig, SLOrcacleState self, float influence)
	{
		orig.Invoke(self, influence);
		FixInfluenceCap(self);
	}

	private void SLOrcacleState_FromString(On.SLOrcacleState.orig_FromString orig, SLOrcacleState self, string s)
	{
		orig.Invoke(self, s);
		FixInfluenceCap(self);
	}

	//2. FIXING "STOLEN-ENLIGHTMENT BUG"

	public static int EncTresh = 0;
	public static int NeuronTresh = 5;

	public static void SetTreshholds(int encs, int neurons) //for KA compatibility
	{
		EncTresh = encs;
		NeuronTresh = neurons;
	}

	private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
	{
		if (self.manager.upcomingProcess != null)
		{
			return;
		}
		Debug.Log("MALNOURISHED: " + malnourished);
		if (!malnourished && !self.rainWorld.saveBackedUp)
		{
			self.rainWorld.saveBackedUp = true;
			self.rainWorld.progression.BackUpSave("_Backup");
		}
		DreamsState dreamsState = self.GetStorySession.saveState.dreamsState;
		if (self.manager.rainWorld.progression.miscProgressionData.starvationTutorialCounter > -1)
		{
			self.manager.rainWorld.progression.miscProgressionData.starvationTutorialCounter++;
		}

		if (self.GetStorySession.saveState.miscWorldSaveData.privSlOracleState != null && self.GetStorySession.saveState.miscWorldSaveData.privSlOracleState.playerEncounters > EncTresh)
		{
			if (!self.GetStorySession.lastEverMetMoon)
			{
				self.manager.CueAchievement((self.GetStorySession.saveState.miscWorldSaveData.SLOracleState.neuronsLeft < NeuronTresh) ? RainWorld.AchievementID.MoonEncounterBad : RainWorld.AchievementID.MoonEncounterGood, 5f);
				if (dreamsState != null)
				{
					dreamsState.InitiateEventDream((self.GetStorySession.saveState.miscWorldSaveData.SLOracleState.neuronsLeft < NeuronTresh) ? DreamsState.DreamID.MoonThief : DreamsState.DreamID.MoonFriend);
				}
			}
			else if (dreamsState != null && !dreamsState.everAteMoonNeuron && self.GetStorySession.saveState.miscWorldSaveData.SLOracleState.neuronsLeft < NeuronTresh)
			{
				dreamsState.InitiateEventDream(DreamsState.DreamID.MoonThief);
			}
		}

		if (!self.GetStorySession.lastEverMetPebbles && self.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad > 0)
		{
			self.manager.CueAchievement(RainWorld.AchievementID.PebblesEncounter, 5f);
			if (self.StoryCharacter == 2)
			{
				self.manager.rainWorld.progression.miscProgressionData.redHasVisitedPebbles = true;
			}
			if (dreamsState != null)
			{
				dreamsState.InitiateEventDream(DreamsState.DreamID.Pebbles);
			}
		}
		if (dreamsState != null)
		{
			dreamsState.EndOfCycleProgress(self.GetStorySession.saveState, self.world.region.name, self.world.GetAbstractRoom(self.Players[0].pos).name);
		}
		self.GetStorySession.saveState.SessionEnded(self, true, malnourished);
		self.manager.RequestMainProcessSwitch((dreamsState == null || !dreamsState.AnyDreamComingUp) ? ProcessManager.ProcessID.SleepScreen : ProcessManager.ProcessID.Dream);
	}

}