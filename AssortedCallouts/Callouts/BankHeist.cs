//TODO: 
// - make sure all spawning f() assign entity.Instance field

using Albo1125.Common.CommonLibrary;
using AssortedCallouts.Extensions;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using Rage.Native;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ExtensionMethods = Albo1125.Common.CommonLibrary.ExtensionMethods;

namespace AssortedCallouts.Callouts
{
    [CalloutInfo("Bank Heist", CalloutProbability.Low)]
    internal partial class BankHeist : AssortedCallout
    {
        private Vector3 PlayerPosition => Game.LocalPlayer.Character.Position;
        private Ped Player => Game.LocalPlayer.Character;
        private float DistToPlayer(Vector3 pos) => Vector3.Distance(PlayerPosition, pos);
        private void DeleteEntity(Entity e) { if (e.Exists()) e.Delete(); }

        private Vector3 PacificBank = new Vector3(231.5777f, 215.1532f, 106.2815f);
        private const ulong _DOOR_CONTROL = 0x9b12f9a24fabedb0;

        private List<Ped> Robbers = new List<Ped>();
        private bool fighting = false;
        private int RobbersKilled = 0;

        private bool CalloutRunning = false;

        //AUDIO
        private System.Media.SoundPlayer AlarmPlayer = new System.Media.SoundPlayer("LSPDFR/Police Scanner/Assorted Callouts Audio/ALARM_BELL.wav");
        private bool AudioStateChanged = false;
        private bool AlarmPlaying = false;
        private enum AudioState { Alarm, None };
        private AudioState CurrentAudioState = AudioState.None;
        private const float DIST_PLAY_BANK_ALARM_ON = 55f;
        private const float DIST_PLAY_BANK_ALARM_OFF = 70f;

        private bool TalkedToWells = false;
        private bool HandlingRespawn = false;
        //NOTE: var stays unchanged
        private bool EvaluatedWithWells = false;

        private int TimesDied = 0;
        private int FightingPacksUsed = 0;
        private bool DoneFighting = false;
        private Blip SideDoorBlip;
        private bool TalkedToWells2nd = false;
        private bool NegotiationResultSurrender;
        private bool Surrendering = false;
        private bool SWATFollowing = false;
        private int SWATUnitsdied = 0;

        private bool FightingPrepared = false;
        private bool SurrenderComplete = false;

        

        private Vector3 HostageSafeLocation = new Vector3(241.8676f, 176.3772f, 105.1341f);
        private float HostageSafeHeading = 158.8192f;

        private List<Vector3> PoliceOfficersStandingLocations = new List<Vector3>() { new Vector3(215.3605f, 199.1968f, 105.542f), new Vector3(214.3272f, 203.5561f, 105.4791f), new Vector3(239.7187f, 189.4415f, 105.2328f), new Vector3(217.9366f, 215.7689f, 105.5233f) };
        private List<float> PoliceOfficersStandingHeadings = new List<float>() { 116.6588f, 121.1613f, 154.7706f, 66.40781f };
        private List<Ped> PoliceOfficersStandingSpawned = new List<Ped>();

        private List<EntityData<Ped>> OfficersStanding = new List<EntityData<Ped>>();




        private List<Vector3> PoliceOfficersAimingLocations = new List<Vector3>() { new Vector3(215.3038f, 210.3652f, 105.5509f), new Vector3(229.6182f, 192.2897f, 105.4265f), new Vector3(223.2215f, 194.5566f, 105.5815f), new Vector3(242.2608f, 188.373f, 105.1962f), new Vector3(252.175f, 189.5349f, 104.8857f), new Vector3(221.073f, 221.157f, 105.4611f) };
        private List<float> PoliceOfficersAimingHeadings = new List<float>() { 284.9829f, 352.2892f, 338.3747f, 302.2641f, 333.9143f, 237.1773f };
        private List<Ped> PoliceOfficersAimingSpawned = new List<Ped>();

        private List<EntityData<Ped>> OfficersAiming = new List<EntityData<Ped>>();


        private List<Ped> PoliceOfficersArresting = new List<Ped>();

        private List<Ped> PoliceOfficersSpawned = new List<Ped>();
        private List<Ped> PoliceOfficersTargetsToShoot = new List<Ped>();

        //Swat teams
        //private List<Ped> SWATTeam1 = new List<Ped>();
        private List<Vector3> SWATTeam1Locations = new List<Vector3>() { new Vector3(260.5645f, 200.5741f, 104.9401f), new Vector3(262.1003f, 200.0121f, 104.9125f), new Vector3(256.6042f, 202.0044f, 105.0125f), new Vector3(255.1498f, 202.5428f, 105.0388f), new Vector3(253.9746f, 203.0882f, 105.0599f), new Vector3(263.3704f, 199.6684f, 104.8904f) };
        private List<float> SWATTeam1Headings = new List<float>() { 71.64834f, 68.8295f, 251.649f, 248.7861f, 248.8271f, 68.8268f };

        //private List<Ped> SWATTeam2 = new List<Ped>();
        private List<Vector3> SWATTeam2Locations = new List<Vector3>() { new Vector3(230.4205f, 222.8963f, 105.5488f), new Vector3(229.3888f, 219.9169f, 105.5496f), new Vector3(230.0146f, 221.7818f, 105.549f), new Vector3(234.2444f, 210.1959f, 105.4067f), new Vector3(235.6489f, 209.7039f, 105.3825f), new Vector3(236.8931f, 209.2961f, 105.3615f) };
        private List<float> SWATTeam2Headings = new List<float>() { 159.7311f, 159.7311f, 159.7311f, 68.82679f, 68.82679f, 68.82679f };

        private List<EntityData<Ped>> SWATOperators = new List<EntityData<Ped>>();

        private int AliveHostagesCount = 0;
        private int SafeHostagesCount = 0;
        private int TotalHostagesCount = 0;

        private Vector3 MiniGunRobberLocation = new Vector3(267.0747f, 224.5822f, 110.2829f);
        private Vector3 MiniGunFireLocation = new Vector3(257.7627f, 223.3801f, 106.2863f);
        private float MiniGunRobberHeading = 92.93042f;
        private Ped MiniGunRobber;
        private bool MiniGunRobberFiring = false;
        private Vector3 BehindGlassDoorLocation = new Vector3(265.4374f, 217.0231f, 110.283f);

        private Ped Maria;
        private Model MariaModel = new Model("A_F_Y_EASTSA_01");

        private Ped MariaCop;
        private Vector3 MariaSpawnPoint = new Vector3(179.9932f, 115.8559f, 94.61918f);
        private float MariaSpawnHeading = 338.6956f;
        private Vector3 MariaCopDestination = new Vector3(235.0675f, 180.2976f, 104.8821f);
        private Vehicle MariaCopCar;
        //Robbers when assault
        private List<Vector3> RobbersAssaultLocations = new List<Vector3>() { new Vector3(267.0549f, 221.0715f, 110.283f), new Vector3(238.9634f, 234.4014f, 108.0783f), new Vector3(237.994f, 225.1579f, 110.2827f), new Vector3(254.5219f, 209.5221f, 110.283f), new Vector3(263.1249f, 208.023f, 106.2832f), new Vector3(259.1342f, 209.493f, 106.2832f), new Vector3(262.4411f, 208.0968f, 110.2865f),
        new Vector3(254.8814f, 226.924f, 101.7847f), new Vector3(261.9492f, 203.3163f, 106.2832f)};
        private List<float> RobbersAssaultHeadings = new List<float>() { 62.3679f, 167.0135f, 251.0168f, 339.1998f, 150.2972f, 238.0847f, 61.37086f, 237.4072f, 69.37666f };

        //When surrender
        private List<Vector3> RobbersSurrenderLocations = new List<Vector3>() { new Vector3(230.8764f, 207.1935f, 105.4408f), new Vector3(233.9847f, 206.0195f, 105.3878f), new Vector3(237.1756f, 205.175f, 105.3347f), new Vector3(239.5613f, 203.8999f, 105.2908f), new Vector3(242.2058f, 203.3801f, 105.2473f), new Vector3(245.231f, 201.896f, 105.1919f), new Vector3(248.4907f, 201.2163f, 105.1362f) };
        private List<float> RobbersSurrenderHeadings = new List<float>() { 146.3448f, 148.8101f, 152.9936f, 175.8113f, 170.111f, 151.4013f, 138.7258f };

        //Captain Wells
        private Ped CaptainWells;
        private Blip CaptainWellsBlip;
        private Vector3 CaptainWellsLocation = new Vector3(261.6116f, 192.8469f, 104.8786f);
        private float CaptainWellsHeading = 49.73652f;
        
        //TODO:
        // - use to delete all entities in End()
        private readonly List<Entity> AllBankHeistEntities = new List<Entity>();

        public override bool OnBeforeCalloutDisplayed()
        {
            SpawnPoint = PacificBank;

            float dist = Vector3.Distance(SpawnPoint, PlayerPosition);

            if (dist < 90f || dist > 2800f)
            {
                return false;
            }

            ShowCalloutAreaBlipBeforeAccepting(SpawnPoint, 15f);
            CalloutPosition = SpawnPoint;
            CalloutMessage = "Pacific Bank Heist";

            ComputerPlusRunning = AssortedCalloutsHandler.IsLSPDFRPluginRunning("ComputerPlus", new Version("1.3.0.0"));
            if (ComputerPlusRunning)
            {
                CalloutID = API.ComputerPlusFuncs.CreateCallout("Pacific Bank Heist", "Bank Heist", SpawnPoint, 1, "Reports of a major bank heist at the Pacific Bank. Multiple emergency services on scene. Respond as a tactical commander.",
                1, null, null);
            }
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + AssortedCalloutsHandler.DivisionUnitBeatAudioString + " WE_HAVE CRIME_BANKHEIST IN_OR_ON_POSITION ", SpawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            AlarmPlayer.Load();

            if (Player.IsInAnyVehicle(false))
            {
                AllBankHeistEntities.Add(Player.CurrentVehicle);
            }

            CalloutHandler();

            return base.OnCalloutAccepted();
        }
        
        public override void Process()
        {
            base.Process();

            proc.Process();

            if (CalloutRunning)
            {
                if (!HandlingRespawn && Player.IsDead)
                {
                    HandleCustomRespawn();
                }
            }
        }
        //STATUS: old f()
        [Obsolete]
        private void CalloutHandler()
        {
            CalloutRunning = true;

            GameFiber.StartNew(delegate
            {
                try
                {
                    SuspectBlip = new Blip(PacificBank);
                    SuspectBlip.IsRouteEnabled = true;

                    SideDoorBlip = new Blip(new Vector3(258.3625f, 200.4897f, 104.9758f));

                    GameFiber.StartNew(delegate
                    {
                        GameFiber.Wait(4800);
                        Game.DisplayNotification("Copy that, responding ~b~CODE 3 ~s~to the ~b~Pacific Bank~s~, over.");
                        Functions.PlayScannerAudio("COPY_THAT_MOVING_RIGHT_NOW REPORT_RESPONSE_COPY PROCEED_WITH_CAUTION_ASSORTED");
                        GameFiber.Wait(3400);
                        Game.DisplayNotification("Roger that, ~r~proceed with caution!");
                    });

                    LoadModels();

                    while (Vector3.Distance(PlayerPosition, SpawnPoint) > 350f)
                    {
                        GameFiber.Yield();
                    }

                    if (Player.IsInAnyVehicle(false))
                    {
                        AllBankHeistEntities.Add(Player.CurrentVehicle);

                        Ped[] passengers = Game.LocalPlayer.Character.CurrentVehicle.Passengers;
                        Array.ForEach(passengers, p => AllBankHeistEntities.Add(p));
                    }

                    GameFiber.Yield();

                    CreateSpeedZone();

                    ClearUnrelatedEntities();

                    Game.LogTrivial("Unrelated entities cleared");
                    GameFiber.Yield();

                    SpawnAllBarriers();

                    SpawnAllPoliceCars();
                    GameFiber.Yield();

                    SpawnBothSwatTeams();
                    GameFiber.Yield();

                    SpawnNegotiationRobbers();

                    GameFiber.Yield();
                    SpawnAllPoliceOfficers();


                    GameFiber.Yield();


                    SpawnSneakyRobbers();

                    SpawnHostages();
                    GameFiber.Yield();

                    SpawnEMSAndFire();

                    GameFiber.Yield();

                    if (AssortedCalloutsHandler.rnd.Next(10) < 2)
                    {
                        SpawnVaultRobbers();
                        proc.ActivateProcess(HandleVaultRobbers);
                    }

                    Game.LogTrivial("Done spawning");

                    MakeNearbyPedsFlee();

                    SneakyRobbersAI();

                    HandleHostages();

                    HandleOpenBackRiotVan();


                    HandleAudio();

                    Game.LogTrivial("Initialisation complete, entering loop");

                    while (CalloutRunning)
                    {
                        GameFiber.Yield();

                        //Constants
                        Game.LocalPlayer.Character.CanAttackFriendlies = false;

                        SetRelationshipGroups(Relationship.Respect);

                        Game.LocalPlayer.Character.IsInvincible = false;

                        SetWeaponModifiersForPlayer(0.92f);

                        DoorControl();

                        //When player has just arrived
                        if (!TalkedToWells && !fighting)
                        {
                            if (!Player.IsInAnyVehicle(false))
                            {
                                if (DistToPlayer(CaptainWells.Position) < 4f)
                                {
                                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                                    {
                                        TalkedToWells = true;
                                        if (ComputerPlusRunning)
                                        {
                                            API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Spoken with Captain Wells.");
                                        }
                                        DetermineInitialDialogue();
                                    }
                                }
                                else
                                {
                                    Game.DisplayHelp("~h~Officer, please report to ~g~Captain Wells ~s~for briefing.");
                                }
                            }
                        }

                        //If fighting is initialised
                        if (!FightingPrepared)
                        {
                            if (fighting)
                            {
                                if (ComputerPlusRunning)
                                {
                                    API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Preparing to enter the bank with SWAT.");
                                }

                                SpawnAssaultRobbers();

                                SpawnMiniGunRobber();

                                CopFightingAI();

                                RobbersFightingAI();

                                CheckForRobbersOutside();

                                FightingPrepared = true;
                            }
                        }

                        //If player talks to cpt wells during fight
                        if (fighting)
                        {
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(PlayerPosition, CaptainWells.Position) < 3f)
                                {
                                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                                    {
                                        SpeechHandler.CptWellsLineAudioCount = 24;
                                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Cpt. Wells: Go on! There are still hostages in there!" }, LineFolderModifier: "Assault", WaitAfterLastLine: false);
                                    }
                                }
                            }
                        }


                        //TODO: NEXT TO REVIEW

                        //Make everyone fight if player enters bank
                        if (!fighting && !Surrendering)
                        {
                            //NOTE: assign gives less overhead than read->compare(->assign)
                            fighting = PacificBankInsideChecks.Any(c => Vector3.Distance(c, PlayerPosition) < 2.3f);
                        }
                        //If all hostages rescued break
                        if (SafeHostagesCount == AliveHostagesCount)
                        {
                            break;
                        }

                        //If surrendered
                        if (SurrenderComplete)
                        {
                            if (ComputerPlusRunning)
                            {
                                API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Robbers have surrendered. Going in to save hostages.");
                            }
                            break;
                        }

                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.FollowKey))
                        {
                            SWATFollowing = !SWATFollowing;
                            string s = SWATFollowing ? "now" : "no longer";
                            Game.DisplaySubtitle($"The ~b~SWAT Units ~s~are {s} following you.", 3000);
                        }
                        if (SWATFollowing)
                        {
                            if (Game.LocalPlayer.Character.IsShooting)
                            {
                                SWATFollowing = false;
                                Game.DisplaySubtitle("The ~b~SWAT Units ~s~are no longer following you.", 3000);
                                Game.LogTrivial("Follow off - shooting");
                            }
                        }
                    }

                    //When surrendered
                    if (SurrenderComplete)
                    {
                        CopFightingAI();
                    }

                    while (CalloutRunning)
                    {
                        GameFiber.Yield();

                        SetRelationshipGroups(Relationship.Companion);

                        Game.LocalPlayer.Character.IsInvincible = false;

                        SetWeaponModifiersForPlayer(0.93f);

                        DoorControl();

                        //If all host rescued

                        if (SafeHostagesCount == AliveHostagesCount)
                        {
                            GameFiber.Wait(3000);
                            break;
                        }
                        if (ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.FollowKey))
                        {
                            SWATFollowing = !SWATFollowing;

                            if (SWATFollowing)
                            {
                                Game.DisplaySubtitle("The ~b~SWAT Units ~s~are following you.", 3000);
                            }
                            else
                            {
                                Game.DisplaySubtitle("The ~b~SWAT Units ~s~are no longer following you.", 3000);
                            }
                        }
                        if (SWATFollowing)
                        {
                            if (Game.LocalPlayer.Character.IsShooting)
                            {
                                SWATFollowing = false;
                                Game.DisplaySubtitle("The ~b~SWAT Units ~s~are no longer following you.", 3000);
                                Game.LogTrivial("Follow off - shooting");
                            }
                        }

                        if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            if (DistToPlayer(CaptainWells.Position) < 4f)
                            {
                                Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");

                                if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                                {
                                    if (!TalkedToWells2nd)
                                    {
                                        List<string> CptWellsSurrenderedDialogue = new List<string>() { "Cpt. Wells: Amazing job, officer! It seems the robbers surrendered!", "Cpt. Wells: Your job now is to rescue all the hostages from the bank.", "Cpt. Wells: Please take care, you never know what the robbers left inside.", "Cpt. Wells: We have no idea if there are still robbers inside.", "You: Roger that, sir. This situation will be over in no time!", "You: Where can I get geared up?", "Cpt. Wells: There's gear in the back of the riot vans." };
                                        SpeechHandler.HandleBankHeistSpeech(CptWellsSurrenderedDialogue);
                                        TalkedToWells2nd = true;
                                        fighting = true;
                                        Game.DisplayNotification("Press ~b~" + AssortedCalloutsHandler.FollowKey + " ~s~to make the SWAT teams follow you.");
                                    }
                                    else
                                    {
                                        SpeechHandler.CptWellsLineAudioCount = 24;
                                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Cpt. Wells: Go on! There are still more hostages in there!" }, LineFolderModifier: "Assault", WaitAfterLastLine: false);
                                    }
                                }
                            }
                            else
                            {
                                if (!TalkedToWells2nd)
                                {
                                    Game.DisplayHelp("~h~Officer, please report to ~g~Captain Wells.");
                                }
                            }
                        }
                    }

                    //The end
                    SWATFollowing = false;
                    DoneFighting = true;
                    CurrentAudioState = AudioState.None;
                    AudioStateChanged = true;

                    while (CalloutRunning)
                    {
                        GameFiber.Yield();

                        DoorControl();

                        if (!EvaluatedWithWells)
                        {
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (DistToPlayer(CaptainWells.Position) < 4f)
                                {
                                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");
                                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                                    {
                                        TalkedToWells = true;
                                        FinalDialogue();
                                        GameFiber.Wait(4000);
                                        DetermineResults();

                                        GameFiber.Wait(9000);
                                        break;
                                    }
                                }
                                else
                                {
                                    Game.DisplayHelp("~h~Talk to ~g~Captain Wells~s~.");
                                }
                            }
                        }
                    }

                    if (CalloutRunning)
                    {
                        Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                        Game.DisplayNotification("~o~Bank Heist ~s~callout is ~g~CODE 4.");
                        CalloutFinished = true;
                    }

                    End();
                }
                catch (Exception e)
                {
                    Game.LogTrivial(e.ToString());
                    End();
                }
            });
        }

        //STAGES APPROACH========================================================================
        private const float DIST_SPAWN_ENTITIES = 350f;
        private readonly ProcessHost proc = new ProcessHost();

        private void IsPlayerAtScene()
        {
            if (DistToPlayer(SpawnPoint) < DIST_SPAWN_ENTITIES)
            {
                if (Player.IsInAnyVehicle(false))
                {
                    AllBankHeistEntities.Add(Player.CurrentVehicle);
                    AllBankHeistEntities.AddRange(Player.CurrentVehicle.Passengers);
                }

                proc.ActivateProcess(CreateSpeedZone_Process);

                ClearUnrelatedEntities();

                SpawnAllBarriers();

                SpawnAllPoliceCars();

                SpawnBothSwatTeams();

                SpawnNegotiationRobbers();

                SpawnAllPoliceOfficers();

                SpawnSneakyRobbers();

                SpawnHostages();

                SpawnEMSAndFire();

                if (AssortedCalloutsHandler.rnd.Next(10) < 2)
                {
                    //TODO: NEXT TO REVIEW
                    SpawnVaultRobbers();
                    proc.ActivateProcess(HandleVaultRobbers);
                }

                proc.ActivateProcess(MakeNearbyPedsFlee);

                proc.ActivateProcess(SneakyRobbersAI);

                proc.ActivateProcess(SneakyRobberCanInitFight_Process);

                proc.ActivateProcess(SneakyRobberFightInProgress);

                proc.ActivateProcess(HandleEndangeredHostages);

                proc.ActivateProcess(HandleRescuedHostages);

                proc.ActivateProcess(HandleSafeHostages);

                proc.ActivateProcess(HandleOpenBackRiotVan);

                proc.ActivateProcess(HandleAudio);

                proc.ActivateProcess(KeepPlayersFeatures);

                proc.ActivateProcess(DoorControl);

                proc.ActivateProcess(TalkToWellsOnArrival);

                proc.ActivateProcess(TalkToWellsDuringFight);

                proc.ActivateProcess(PrepareFighting);

                //decide what function use as-is and which should remodeled to stages

                //swap
            }
        }

        private void ClearUnrelatedEntities()
        {
            var entitiesToRemove = World.GetEntities(SpawnPoint, 50f, GetEntitiesFlags.ConsiderAllPeds | GetEntitiesFlags.ConsiderAllVehicles).ToList();
            Func<Entity, bool> selector = p => p.Exists() && p.IsValid() && p != Player && !p.CreatedByTheCallingPlugin && !AllBankHeistEntities.Contains(p) && p != Player.CurrentVehicle;
            entitiesToRemove.ForEach(e => { if (selector(e)) e.Delete(); });
        }

        private void SpawnAllBarriers()
        {
            Barriers_.ForEach(b =>
            {
                b.Instance = PlaceBarrier(b.Position, b.Heading);
                b.InvisibleWall = CreateInvisibleWallForBarrier(b.Position, b.Heading);
                b.Ped = BarrierPed(b.Position);
                AllBankHeistEntities.Add(b.Instance);
            });
        }

        private Rage.Object PlaceBarrier(Vector3 Location, float Heading)
        {
            Rage.Object Barrier = new Rage.Object("prop_barrier_work05", Location);
            Barrier.Heading = Heading;
            Barrier.IsPositionFrozen = true;
            Barrier.IsPersistent = true;
            return Barrier;
        }

        private Rage.Object CreateInvisibleWallForBarrier(Vector3 pos, float head)
        {
            Rage.Object invWall = new Rage.Object("p_ice_box_01_s", pos, head);
            invWall.IsVisible = false;
            invWall.IsPersistent = true;
            return invWall;
        }

        private Ped BarrierPed(Vector3 pos)
        {
            Ped invPed = new Ped(pos);
            invPed.IsVisible = false;
            invPed.IsPositionFrozen = true;
            invPed.BlockPermanentEvents = true;
            invPed.IsPersistent = true;
            return invPed;
        }

        private void KeepPlayersFeatures()
        {
            Player.CanAttackFriendlies = false;

            SetRelationshipGroups(Relationship.Respect);

            Player.IsInvincible = false;

            SetWeaponModifiersForPlayer(0.92f);
        }

        private void TalkToWellsOnArrival()
        {
            if (!fighting)
            {
                if (!Player.IsInAnyVehicle(false))
                {
                    if (DistToPlayer(CaptainWells.Position) < 4f)
                    {
                        Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");

                        if (ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                        {
                            proc.DeactivateProcess(TalkToWellsOnArrival);

                            if (ComputerPlusRunning)
                            {
                                API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Spoken with Captain Wells.");
                            }

                            DetermineInitialDialogue();
                        }
                    }
                    else
                    {
                        Game.DisplayHelp("~h~Officer, please report to ~g~Captain Wells ~s~for briefing.");
                    }
                }
            }
        }

        private void TalkToWellsDuringFight()
        {
            if (fighting)
            {
                if (!Player.IsInAnyVehicle(false))
                {
                    if (DistToPlayer(CaptainWells.Position) < 3f)
                    {
                        Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to talk.");

                        if (ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                        {
                            SpeechHandler.CptWellsLineAudioCount = 24;
                            SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Cpt. Wells: Go on! There are still hostages in there!" }, LineFolderModifier: "Assault", WaitAfterLastLine: false);
                        }
                    }
                }
            }
        }

        private void PrepareFighting()
        {
            if (fighting)
            {
                if (ComputerPlusRunning)
                {
                    API.ComputerPlusFuncs.AddUpdateToCallout(CalloutID, "Preparing to enter the bank with SWAT.");
                }

                SpawnAssaultRobbers();

                SpawnMiniGunRobber();

                CopFightingAI();

                RobbersFightingAI();

                CheckForRobbersOutside();

                proc.DeactivateProcess(PrepareFighting);
            }
        }

        private void CreateSpeedZone_Process()
        {
            var vehs = World.GetEntities(SpawnPoint, 75f, GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ExcludePoliceCars | GetEntitiesFlags.ExcludeFiretrucks | GetEntitiesFlags.ExcludeAmbulances);
            foreach (Vehicle veh in vehs)
            {
                if (AllBankHeistEntities.Contains(veh))
                {
                    continue;
                }

                if (!veh.Exists() || veh == Player.CurrentVehicle || veh.CreatedByTheCallingPlugin)
                {
                    continue;
                }

                if (veh.Velocity.Length() > 0f)
                {
                    Vector3 velocity = veh.Velocity;
                    velocity.Normalize();
                    velocity *= 0f;
                    veh.Velocity = velocity;
                }
            }
        }

        //STAGES APPROACH END========================================================================
        
            //STATUS: unchanged, incorporate into spawning f()'s?
        private void LoadModels()
        {
            foreach (string s in LSPDModels)
            {
                GameFiber.Yield();
                new Model(s).Load();
            }
            foreach (string s in HostageModels)
            {
                GameFiber.Yield();
                new Model(s).Load();
            }
            new Model("s_m_y_robber_01").Load();
        }

        private Rage.Object MobilePhone;
        //STATUS: UNCHANGED
        private void ToggleMobilePhone(Ped ped, bool toggle)
        {
            if (toggle)
            {
                NativeFunction.Natives.SET_PED_CAN_SWITCH_WEAPON(ped, false);
                ped.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_UNARMED"), -1, true);
                MobilePhone = new Rage.Object(new Model("prop_police_phone"), new Vector3(0, 0, 0));
                int boneIndex = NativeFunction.Natives.GET_PED_BONE_INDEX<int>(ped, (int)PedBoneId.RightPhHand);
                NativeFunction.Natives.ATTACH_ENTITY_TO_ENTITY(MobilePhone, ped, boneIndex, 0f, 0f, 0f, 0f, 0f, 0f, true, true, false, false, 2, 1);
                ped.Tasks.PlayAnimation("cellphone@", "cellphone_call_listen_base", 1.3f, AnimationFlags.Loop | AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask);
            }
            else
            {
                NativeFunction.Natives.SET_PED_CAN_SWITCH_WEAPON(ped, true);
                ped.Tasks.Clear();

                if (GameFiber.CanSleepNow)
                {
                    GameFiber.Wait(800);
                }

                DeleteEntity(MobilePhone);
            }
        }
        //STATUS: UNCHANGED
        private void GetMaria()
        {
            Game.LocalPlayer.Character.IsPositionFrozen = true;
            MariaCopCar = new Vehicle("POLICE", MariaSpawnPoint, MariaSpawnHeading);

            MariaCopCar.IsSirenOn = true;
            MariaCop = MariaCopCar.CreateRandomDriver();
            MariaCop.MakeMissionPed();
            Maria = new Ped(MariaModel, MariaSpawnPoint, MariaSpawnHeading);
            Maria.MakeMissionPed();
            Maria.WarpIntoVehicle(MariaCopCar, 0);
            AllBankHeistEntities.Add(Maria);
            AllBankHeistEntities.Add(MariaCop);
            AllBankHeistEntities.Add(MariaCopCar);

            MariaCop.Tasks.DriveToPosition(MariaCopDestination, 20f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
            while (true)
            {
                GameFiber.Yield();
                if (Vector3.Distance(MariaCopCar.Position, MariaCopDestination) < 6f)
                {
                    break;
                }
            }
            Maria.Tasks.LeaveVehicle(LeaveVehicleFlags.None);
            Maria.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeRight * 1.5f), Game.LocalPlayer.Character.Heading, 1.9f).WaitForCompletion(60000);
            Game.LocalPlayer.Character.IsPositionFrozen = false;
        }
        //STATUS: UNCHANGED
        private void NegotiationIntro()
        {
            SpeechHandler.YouLineAudioCount = 1;
            ToggleMobilePhone(Game.LocalPlayer.Character, true);
            GameFiber.Wait(2000);
            SpeechHandler.PlayPhoneCallingSound(2);
            NegotiationResultSurrender = false;
            List<string> IntroLines = new List<string>() { "Robber: Who the hell is this? I'm busy!", "You: I'm an officer with the LSPD. Who am I speaking to?", "Robber: Shut up! I'm in control here!" };
            SpeechHandler.HandleBankHeistSpeech(IntroLines, LineFolderModifier: "NegotiationIntro");
            List<string> IntroAnswers = new List<string>() { "You: Look, the bank is surrounded, we all want to go home, just come out peacefully.", "You: OK, what do you want?", "You: Can't we make a deal?" };

            int res = SpeechHandler.DisplayAnswers(IntroAnswers);

            if (res == 0)
            {
                SpeechHandler.HandleBankHeistSpeech(new List<string>() { IntroAnswers[res] }, LineFolderModifier: "NegotiationOne");
                NegotiationOne();
            }
            else if (res == 1)
            {
                SpeechHandler.HandleBankHeistSpeech(new List<string>() { IntroAnswers[res] }, LineFolderModifier: "NegotiationTwo");
                NegotiationTwo();
            }
            else if (res == 2)
            {
                SpeechHandler.HandleBankHeistSpeech(new List<string>() { IntroAnswers[res] }, LineFolderModifier: "NegotiationThree");
                NegotiationThree();
            }
            if (!Maria.Exists())
            {
                SpeechHandler.PlayPhoneBusySound(3);
            }
            ToggleMobilePhone(Game.LocalPlayer.Character, false);

            GameFiber.Wait(2000);
            if (NegotiationResultSurrender)
            {
                NegotiationRobbersSurrender();
                return;
            }
            else
            {
                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to signal the SWAT teams to move in.");

                    if (ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "~b~You: ~s~SWAT Team Alpha, ~g~green light!~s~ Move in!" }, WaitAfterLastLine: false);
                        Game.DisplayNotification("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.FollowKey) + " ~s~to make the SWAT teams follow you.");
                        fighting = true;
                        break;
                    }
                }
            }
        }
        //STATUS: UNCHANGED
        private void NegotiationOne()
        {
            List<string> Robber1 = new List<string>() { "Robber: Fuck you! I'll kill a hostage if you come anywhere near that door!" };
            SpeechHandler.HandleBankHeistSpeech(Robber1, LineFolderModifier: "NegotiationOne");
            List<string> Answers1 = new List<string>() { "You: Calm down. We don't want anyone to get hurt.", "You: OK. What do you want?", "You: You do that and every cop in the city will be in that bank!" };
            int res1 = SpeechHandler.DisplayAnswers(Answers1);

            //*Player Response Options To Robber Response #1 (Final Set):*
            if (res1 == 0)
            {
                SpeechHandler.YouLineAudioCount = 3;
                List<string> RobberResponse1 = new List<string>() { Answers1[res1], "Robber: Don't tell me what to do pig!" };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationOne");
                List<string> AnswersToRes1 = new List<string>() { "You: Look we all want to get out of this alive, just surrender. Do it for your wife, Maria.", "You: I'm sorry, I just don't want anyone to get hurt.", "You: I'm trying to save everyone here." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 6;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: How do you know about her?! Leave my family alone!" }, LineFolderModifier: "NegotiationOne");
                    //Fight
                    NegotiationResultSurrender = false;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 7;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Unfortunatly people are going to." }, LineFolderModifier: "NegotiationOne");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 8;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: I'm trying to save my family and dying here will only kill them...", "OK, we're coming out." }, LineFolderModifier: "NegotiationOne");
                    //Surrender
                    NegotiationResultSurrender = true;
                }
            }

            //*Player Response Options To Robber Response #2 (Final Set)

            else if (res1 == 1)
            {
                SpeechHandler.YouLineAudioCount = 4;
                List<string> RobberResponse1 = new List<string>() { Answers1[res1], "Robber: We want a bus to take us to Los Santos International Airport!", "Robber: From there, we want a plane to fly us to Liberty City." };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationOne");
                List<string> AnswersToRes1 = new List<string>() { "You: Release the hostages and I'll cut your sentences by 5 years.", "You: I'll get one fuelled up for you, but I need a hostage in return.", "You: I can try, but it might take a while." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: 5 years! We can't go to jail, but we can't die either...", "Robber: We surrender!" }, LineFolderModifier: "NegotiationOne");
                    //Surrender
                    NegotiationResultSurrender = true;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 10;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: We can't do that officer and you know it, you're just wasting our time." }, LineFolderModifier: "NegotiationOne");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 11;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Sorry, we don't have time to wait." }, LineFolderModifier: "NegotiationOne");
                    //Fight
                    NegotiationResultSurrender = false;
                }
            }

            //*Player Response Options To Robber Response #3 (Final Set):*
            else if (res1 == 2)
            {
                SpeechHandler.YouLineAudioCount = 5;
                List<string> RobberResponse1 = new List<string>() { Answers1[res1], "Robber: And every last hostage will be dead before they get in!" };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationOne");
                List<string> AnswersToRes1 = new List<string>() { "You: Don't say things like that, or you might get a meet and greet with a SWAT team.", "You: What would Maria say to you if she heard you say that?", "You: Don't you want to see your family again? Your hostages do, let them go!" };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 12;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Fuck you pig! You and everyone else are going to die!" }, LineFolderModifier: "NegotiationOne");
                    //Fight
                    NegotiationResultSurrender = false;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 13;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: How do you know about Maria?! Fuck off pig!" }, LineFolderModifier: "NegotiationOne");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 14;
                    if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Fuck you pig! You and everyone else are going to die!" }, LineFolderModifier: "NegotiationOne");
                        //Fight
                        NegotiationResultSurrender = false;
                    }
                    else
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Of course I do.... OK, we are coming out. Don't shoot!" }, LineFolderModifier: "NegotiationOne");
                        //Surrender
                        NegotiationResultSurrender = true;
                    }

                }
            }

        }
        //STATUS: UNCHANGED
        private void NegotiationTwo()
        {
            List<string> Robber2 = new List<string>() { "Robber: We want a bus to take us to Los Santos International.", "Robber: From there, we want a plane to fly us to Liberty City." };
            SpeechHandler.HandleBankHeistSpeech(Robber2, LineFolderModifier: "NegotiationTwo");
            List<string> Answers2 = new List<string>() { "You: I'll try to work on it but I can't guarantee anything.", "You: OK, we have a bus on the way, but you need to release the hostages first.", "You: Release the hostages and I'll cut your sentences by 5 years." };
            int res1 = SpeechHandler.DisplayAnswers(Answers2);

            //*Player Response Options To Robber Response #1 (Final Set):*
            if (res1 == 0)
            {
                SpeechHandler.YouLineAudioCount = 3;
                List<string> RobberResponse1 = new List<string>() { Answers2[res1], "Robber: Don't tell me what you might be able to do!" };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationTwo");
                List<string> AnswersToRes1 = new List<string>() { "You: Look, it's the best I can do!", "You: Give me a hostage and you get your bus.", "You: I'm working on it but it could take some time." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 6;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Your best isn't good enough." }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 7;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: How about a bullet instead?" }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 8;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Time is something we don't have." }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;
                }
            }

            //*Player Response Options To Robber Response #2 (Final Set):*
            else if (res1 == 1)
            {
                SpeechHandler.YouLineAudioCount = 4;
                List<string> RobberResponse1 = new List<string>() { Answers2[res1], "Robber: Fuck you! They come out when I see the bus!" };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationTwo");
                List<string> AnswersToRes1 = new List<string>() { "You: The bus won't be there unless we get something from you.", "You: I have your wife Maria here, she wants to talk to you.", "You: It has to go both ways. If you give me a hostage your bus will arrive more quickly." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 9;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: You aren't getting shit from me other than a bullet!" }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;

                }
                else if (resresponsetorobber1 == 1)
                {
                    //50% chance to bring Maria in!
                    SpeechHandler.YouLineAudioCount = 10;
                    if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: You have my wife? Tell her I love her and I'm doing this for our kids." }, LineFolderModifier: "NegotiationTwo");

                        NegotiationResultSurrender = false;
                    }
                    else
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1] }, LineFolderModifier: "NegotiationTwo");
                        SpeechHandler.YouLineAudioCount = 100;
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Robber: Let me talk to her.", "You: Hang on for a minute, she's coming!" }, LineFolderModifier: "NegotiationTwo");
                        GetMaria();
                        ToggleMobilePhone(Game.LocalPlayer.Character, false);
                        ToggleMobilePhone(Maria, true);
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Maria: Baby please come out, think about our kids!", "Maria: Please don't do this, honey!", "Robber: I'm so sorry, Maria...", "Robber: I really don't know why I'm here.", "Robber: I love you, Maria!", "Robber: I'm coming out!" }, LineFolderModifier: "NegotiationTwo");
                        SpeechHandler.PlayPhoneBusySound(3);
                        ToggleMobilePhone(Maria, false);
                        GameFiber.Wait(1500);

                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "You: Thank you, Maria. For your safety, please get back in the police car.", "Maria: Please don't shoot my husband!" }, LineFolderModifier: "NegotiationTwo");
                        Maria.Tasks.FollowNavigationMeshToPosition(MariaCopCar.GetOffsetPosition(Vector3.RelativeRight * 2f), MariaCopCar.Heading, 1.9f).WaitForCompletion(15000);
                        Maria.Tasks.EnterVehicle(MariaCopCar, 5000, 0).WaitForCompletion();
                        MariaCop.Tasks.CruiseWithVehicle(MariaCopCar, 20f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
                        NegotiationResultSurrender = true;
                    }
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Nah, it goes only my way." }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;
                }
            }

            //*Player Response Options To Robber Response #3 (Final Set):*
            else if (res1 == 2)
            {
                SpeechHandler.YouLineAudioCount = 100;
                List<string> RobberResponse1 = new List<string>() { Answers2[res1], "Robber: Look man, we have families and we just need the money.", "Robber: We don't want anyone to get hurt!" };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationTwo");
                List<string> AnswersToRes1 = new List<string>() { "You: Think about your family! Put down your weapons and come out with your hands up!", "You: Think about them! The people you're holding in there have families too!", "You: If you don't want people to get hurt, surrender and save everyone." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    if (AssortedCalloutsHandler.rnd.Next(2) == 0)
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: You aren't getting shit from me other than a bullet!" }, LineFolderModifier: "NegotiationTwo");
                        //Fight
                        NegotiationResultSurrender = false;
                    }
                    else
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: They couldn't survive without me. OK, we are coming out!" }, LineFolderModifier: "NegotiationTwo");
                        //Surrender
                        NegotiationResultSurrender = true;
                    }

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Fuck their families! I need money for MY family!" }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 14;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Sadly people get hurt in this cruel world." }, LineFolderModifier: "NegotiationTwo");
                    //Fight
                    NegotiationResultSurrender = false;
                }
            }
        }
        private void NegotiationThree()
        {
            List<string> Robber3 = new List<string>() { "Robber: Pig, what type of deal could you make that would interest us?!" };
            SpeechHandler.HandleBankHeistSpeech(Robber3, LineFolderModifier: "NegotiationThree");
            List<string> Answers3 = new List<string>() { "You: Look, the bank is surrounded. We all want to go home here, so just come out peacefully!", "You: What are you interested in then?", "You: No idea, but I'm sure we can still make a deal." };
            int res1 = SpeechHandler.DisplayAnswers(Answers3);

            //*Player Response Options To Robber Response #1 (Final Set):*
            if (res1 == 0)
            {
                SpeechHandler.YouLineAudioCount = 3;
                List<string> RobberResponse1 = new List<string>() { Answers3[res1], "Robber: I'll kill a hostage if you come anywhere near that door! " };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationThree");
                List<string> AnswersToRes1 = new List<string>() { "You: Calm down, we don't want anyone to get hurt!", "You: You do that and you may be featured on the morning news.", "You: You don't want to hurt innocent people." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 6;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: People get hurt every day." }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 7;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Interesting propsect, I'll take it!" }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 8;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Innocent? In this world? Hah!" }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;
                }
            }

            //*Player Response Options To Robber Response #2 (Final Set):*
            else if (res1 == 1)
            {
                SpeechHandler.YouLineAudioCount = 100;
                List<string> RobberResponse1 = new List<string>() { Answers3[res1], "Robber: We're interested in a free plane ticket to Liberty City, business class!" };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationThree");
                List<string> AnswersToRes1 = new List<string>() { "You: You know that won't get approved. Don't you want to see your family again?", "You: That might take some time to handle. Giving us a hostage could speed it up!", "You: How about some business class pizza instead?" };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 9;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Of course I do. Alright, we are coming out, don't shoot!" }, LineFolderModifier: "NegotiationThree");
                    //Surrender
                    NegotiationResultSurrender = true;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Time is scarce, and so is human life." }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Pizza? You mean the one topped with hostage heads?" }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;
                }
            }

            //*Player Response Options To Robber Response #3 (Final Set):*
            else if (res1 == 2)
            {
                SpeechHandler.YouLineAudioCount = 100;
                List<string> RobberResponse1 = new List<string>() { Answers3[res1], "Robber: You'd better start making good deals soon then, or someone may die here." };
                SpeechHandler.HandleBankHeistSpeech(RobberResponse1, LineFolderModifier: "NegotiationThree");
                List<string> AnswersToRes1 = new List<string>() { "You: I can give you something you want for a few hostages.", "You: Peacefully give me all the hostages and you'll be OK!", "You: I can shorten all of your sentences and make sure your family sees you if you surrender." };
                int resresponsetorobber1 = SpeechHandler.DisplayAnswers(AnswersToRes1);

                if (resresponsetorobber1 == 0)
                {
                    SpeechHandler.YouLineAudioCount = 12;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: I want this bank's money here!" }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;

                }
                else if (resresponsetorobber1 == 1)
                {
                    SpeechHandler.YouLineAudioCount = 100;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: There is no peace in this world!" }, LineFolderModifier: "NegotiationThree");
                    //Fight
                    NegotiationResultSurrender = false;
                }
                else if (resresponsetorobber1 == 2)
                {
                    SpeechHandler.YouLineAudioCount = 14;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AnswersToRes1[resresponsetorobber1], "Robber: Alright, we'll come out. Just wait a minute..." }, LineFolderModifier: "NegotiationThree");
                    //Fight < Robbers first
                    NegotiationResultSurrender = false;
                }
            }
        }
        private void DetermineInitialDialogue()
        {
            List<string> InitialDialogue = new List<string>() { "You: Sir, what's the situation?", "Cpt. Wells: Well, we have multiple armed suspects in the bank with hostages.", "You: How many robbers are there?", "Cpt. Wells: We don't know how many are in there, officer.", "Cpt. Wells: The way I see it you have two options.", "Cpt. Wells: You could attempt to negotiate with them.", "Cpt. Wells: Alternatively, you can go in with SWAT Team Alpha." };
            SpeechHandler.CptWellsLineAudioCount = 1;
            SpeechHandler.YouLineAudioCount = 1;
            SpeechHandler.HandleBankHeistSpeech(InitialDialogue, LineFolderModifier: "Intro");
            List<string> NegOrAss = new List<string>() { "You: I'm going to try to talk to them first and see how that goes.", "You: Let's take these bastards out! " };
            int result = SpeechHandler.DisplayAnswers(NegOrAss);
            //If negotiate
            if (result == 0)
            {
                SpeechHandler.CptWellsLineAudioCount = 6;
                SpeechHandler.YouLineAudioCount = 3;
                List<string> NegotiationDialogue = new List<string>() { NegOrAss[result], "Cpt. Wells: Alright. The SWAT team will have your back!", "Cpt. Wells: Also, do you want our tech team to kill that alarm?" };
                SpeechHandler.HandleBankHeistSpeech(NegotiationDialogue, LineFolderModifier: "Negotiation");

                int negresult = SpeechHandler.DisplayAnswers(AlarmAnswers);
                if (negresult == 0)
                {
                    SpeechHandler.CptWellsLineAudioCount = 8;
                    SpeechHandler.YouLineAudioCount = 4;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AlarmAnswers[negresult], "Cpt. Wells: Tech team... cut the alarm." }, LineFolderModifier: "Negotiation");

                    AlarmPlayer.Stop();
                    CurrentAudioState = AudioState.None;
                    SpeechHandler.YouLineAudioCount = 5;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Cpt. Wells: Good luck officer. Let's try to end this thing peacefully.", "You: Copy that, sir." }, LineFolderModifier: "Negotiation");
                }
                else if (negresult == 1)
                {
                    SpeechHandler.CptWellsLineAudioCount = 10;
                    SpeechHandler.YouLineAudioCount = 6;
                    CurrentAudioState = AudioState.Alarm;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AlarmAnswers[negresult], "Cpt. Wells: You sure? If you change your mind, radio the tech team and let us know.", "You: Alright, I'm going to get suited up.", "Cpt. Wells: Good luck!" }, LineFolderModifier: "Negotiation");
                }
                Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.ToggleAlarmKey) + " ~s~at any time to toggle the alarm.");
                GameFiber.Wait(4000);
                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to initiate the ~b~negotiation call~s~ with the robbers.");
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                    {
                        break;
                    }

                }
                Game.HideHelp();
                NegotiationIntro();
            }

            //If assault
            else if (result == 1)
            {
                SpeechHandler.CptWellsLineAudioCount = 12;
                SpeechHandler.YouLineAudioCount = 100; //To be changed
                SpeechHandler.HandleBankHeistSpeech(new List<string>() { NegOrAss[result] }, LineFolderModifier: "Assault");
                SpeechHandler.YouLineAudioCount = 8;
                SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Cpt. Wells: Good idea, officer. There's no time for talking.", "Cpt. Wells: The robbers are holding 8 hostages. Rescuing them is the top priority.", "You: Roger that, sir. Where can I get some gear?", "Cpt. Wells: There's gear in the back of the riot vans.", "Cpt. Wells: SWAT Team Alpha is on standby near the doors, let them know when you're ready.", "You: Alright, let's do this! ", "Cpt. Wells: Also, do you want the tech team to kill the alarm?" }, LineFolderModifier: "Assault");
                int alarmres = SpeechHandler.DisplayAnswers(AlarmAnswers);

                if (alarmres == 0)
                {
                    SpeechHandler.YouLineAudioCount = 4;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AlarmAnswers[alarmres], "Cpt. Wells: Tech team... shut it down." }, LineFolderModifier: "Assault");
                    AlarmPlayer.Stop();

                    CurrentAudioState = AudioState.None;
                    SpeechHandler.YouLineAudioCount = 5;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { "Cpt. Wells: Good luck officer, let's get this over with.", "You: Copy that, sir." }, LineFolderModifier: "Assault");
                }
                else if (alarmres == 1)
                {
                    SpeechHandler.CptWellsLineAudioCount = 19;
                    CurrentAudioState = AudioState.Alarm;
                    SpeechHandler.YouLineAudioCount = 6;
                    SpeechHandler.HandleBankHeistSpeech(new List<string>() { AlarmAnswers[alarmres], "Cpt. Wells: You sure? If you change your mind, radio the tech team.", "You: Alright, I'm going to get suited up.", "Cpt. Wells: Good luck!" }, LineFolderModifier: "Assault");
                }
                Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.ToggleAlarmKey) + " ~s~at any time to toggle the alarm.");
                GameFiber.Wait(4500);
                while (CalloutRunning)
                {
                    GameFiber.Yield();
                    Game.DisplayHelp("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.TalkKey) + " ~s~to signal the SWAT teams to move in.");

                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.TalkKey))
                    {
                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "~b~You: ~s~SWAT Team Alpha, ~g~green light!~s~ Move in!" }, WaitAfterLastLine: false);
                        Game.DisplayNotification("Press ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.FollowKey) + " ~s~to make the SWAT teams follow you.");
                        fighting = true;
                        break;
                    }
                }
            }
        }
        //STATUS: UNCHANGED
        private void NegotiationRobbersSurrender()
        {
            SurrenderComplete = false;
            Surrendering = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    Game.DisplayNotification("~b~Cpt. Wells:~s~ The ~r~robbers ~s~seem to be surrendering. Get in position behind a ~b~police car.");
                    GameFiber.Wait(6000);
                    Game.DisplayNotification("~b~Other officers~s~ will perform the ~b~arrests~s~ and then ~b~deal with the robbers.");
                    GameFiber.Wait(6000);
                    Game.DisplayNotification("~b~Hold your position~s~ and keep the robbers under control by ~b~aiming in their direction.");
                    bool AllRobbersAtLocation = false;
                    for (int i = 0; i < Robbers.Count; i++)
                    {
                        GameFiber.Yield();
                        Robbers[i].Tasks.PlayAnimation("random@getawaydriver", "idle_2_hands_up", 1f, AnimationFlags.UpperBodyOnly | AnimationFlags.StayInEndFrame | AnimationFlags.SecondaryTask);
                        Robbers[i].Tasks.FollowNavigationMeshToPosition(RobbersSurrenderLocations[i], RobbersSurrenderHeadings[i], 1.45f);
                        Rage.Native.NativeFunction.Natives.SET_PED_CAN_RAGDOLL(Robbers[i], false);
                    }
                    int waitcount = 0;
                    while (!AllRobbersAtLocation)
                    {
                        GameFiber.Yield();
                        waitcount++;
                        if (waitcount >= 10000)
                        {
                            for (int i = 0; i < Robbers.Count; i++)
                            {
                                Robbers[i].Position = RobbersSurrenderLocations[i];
                                Robbers[i].Heading = RobbersSurrenderHeadings[i];
                            }

                            break;
                        }
                        for (int i = 0; i < Robbers.Count; i++)
                        {
                            GameFiber.Yield();

                            if (Vector3.Distance(Robbers[i].Position, RobbersSurrenderLocations[i]) < 0.8f)
                            {
                                AllRobbersAtLocation = true;
                            }
                            else
                            {
                                AllRobbersAtLocation = false;
                                break;
                            }
                        }

                        foreach (var op in SWATOperators)
                        {
                            GameFiber.Wait(100);
                            Ped robber = Robbers[AssortedCalloutsHandler.rnd.Next(Robbers.Count)];
                            Rage.Native.NativeFunction.Natives.TASK_AIM_GUN_AT_COORD(op, robber.Position.X, robber.Position.Y, robber.Position.Z, -1, false, false);
                        }
                    }

                    GameFiber.Wait(1000);

                    for (int i = 0; i < Robbers.Count; i++)
                    {
                        GameFiber.Yield();

                        Robbers[i].Tasks.PlayAnimation("random@arrests", "kneeling_arrest_idle", 1f, AnimationFlags.Loop);
                        NativeFunction.Natives.SET_PED_DROPS_WEAPON(Robbers[i]);
                        if (PoliceOfficersSpawned.Count >= i + 1)
                        {
                            PoliceOfficersArresting.Add(PoliceOfficersSpawned[i]);
                            PoliceOfficersSpawned[i].Tasks.FollowNavigationMeshToPosition(Robbers[i].GetOffsetPosition(Vector3.RelativeBack * 0.7f), Robbers[i].Heading, 1.55f);
                            Rage.Native.NativeFunction.Natives.SET_PED_CAN_RAGDOLL(PoliceOfficersSpawned[i], false);
                        }

                    }
                    GameFiber.Wait(1000);

                    bool AllArrestingOfficersAtLocation = false;
                    waitcount = 0;
                    while (!AllArrestingOfficersAtLocation)
                    {
                        GameFiber.Yield();
                        waitcount++;
                        if (waitcount >= 10000)
                        {
                            for (int i = 0; i < PoliceOfficersArresting.Count; i++)
                            {
                                PoliceOfficersArresting[i].Position = Robbers[PoliceOfficersSpawned.IndexOf(PoliceOfficersArresting[i])].GetOffsetPosition(Vector3.RelativeBack * 0.7f);
                                PoliceOfficersArresting[i].Heading = Robbers[PoliceOfficersSpawned.IndexOf(PoliceOfficersArresting[i])].Heading;

                            }
                            break;
                        }
                        for (int i = 0; i < PoliceOfficersArresting.Count; i++)
                        {

                            if (Vector3.Distance(PoliceOfficersArresting[i].Position, Robbers[PoliceOfficersSpawned.IndexOf(PoliceOfficersArresting[i])].GetOffsetPosition(Vector3.RelativeBack * 0.7f)) < 0.8f)
                            {
                                AllArrestingOfficersAtLocation = true;
                            }
                            else
                            {
                                PoliceOfficersArresting[i].Tasks.FollowNavigationMeshToPosition(Robbers[PoliceOfficersSpawned.IndexOf(PoliceOfficersArresting[i])].GetOffsetPosition(Vector3.RelativeBack * 0.7f), Robbers[PoliceOfficersSpawned.IndexOf(PoliceOfficersArresting[i])].Heading, 1.55f).WaitForCompletion(500);
                                AllArrestingOfficersAtLocation = false;
                                break;
                            }
                        }
                    }

                    foreach (var op in SWATOperators)
                    {
                        op.Instance.Tasks.Clear();
                    }

                    for (int i = 0; i < Robbers.Count; i++)
                    {
                        Robbers[i].Tasks.PlayAnimation("mp_arresting", "idle", 8f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask | AnimationFlags.Loop);
                        Robbers[i].Tasks.FollowNavigationMeshToPosition(PoliceVehicles[i].Instance.GetOffsetPosition(Vector3.RelativeLeft * 2f), PoliceVehicles[i].Heading, 1.58f);
                        PoliceOfficersArresting[i].Tasks.FollowNavigationMeshToPosition(PoliceVehicles[i].Instance.GetOffsetPosition(Vector3.RelativeLeft * 2f), PoliceVehicles[i].Instance.Heading, 1.55f);
                    }

                    GameFiber.Wait(5000);
                    SurrenderComplete = true;
                    GameFiber.Wait(12000);
                    for (int i = 0; i < Robbers.Count; i++)
                    {
                        Robbers[i].BlockPermanentEvents = true;
                        Robbers[i].Tasks.EnterVehicle(PoliceVehicles[i].Instance, 11000, 1);
                        PoliceOfficersArresting[i].BlockPermanentEvents = true;
                        PoliceOfficersArresting[i].Tasks.EnterVehicle(PoliceVehicles[i].Instance, 11000, -1);
                    }
                    GameFiber.Wait(11100);
                }
                catch (Exception e)
                {
                    Game.LogTrivial(e.ToString());
                }
            });
        }
        //STATUS: UNCHANGED, make a proc
        private void CheckForRobbersOutside()
        {
        //NOTES:
        // - look for a way to LINQ the conditional list.Add
            GameFiber.StartNew(delegate
            {
                while (CalloutRunning)
                {
                    GameFiber.Yield();

                    if (!fighting) continue;
                    foreach (Vector3 Location in PacificBankDoors)
                    {
                        var robbers = World.GetEntities(Location, 1.6f, GetEntitiesFlags.ConsiderAllPeds).ToList();

                        robbers.RemoveAll(r => !r.Exists() || r.IsDead || Vector3.Distance(r.Position, Location) < 1.5f);

                        var robbersOnTheGlobalRobbersList = robbers.FindAll(r => Robbers.Contains(r));

                        foreach (var robber in robbersOnTheGlobalRobbersList)
                        {
                            if (!PoliceOfficersTargetsToShoot.Contains(robber))
                            {
                                PoliceOfficersTargetsToShoot.Add(robber as Ped);
                            }
                        }
                    }
                }
            });
        }
        //STATUS: UNCHANGED
        private void CopsReturnToLocation()
        {
        //NOTES:
        // - make struct to hold ped and it's default standing pos
            //var validCops = PoliceOfficersStandingSpawned.FindAll(o => o.Exists() && o.IsAlive);

            for (int i = 0; i < PoliceOfficersStandingSpawned.Count; i++)
            {
                if (PoliceOfficersStandingSpawned[i].Exists())
                {
                    if (PoliceOfficersStandingSpawned[i].IsAlive)
                    {
                        if (Vector3.Distance(PoliceOfficersStandingSpawned[i].Position, PoliceOfficersStandingLocations[i]) > 0.5f)
                        {
                            PoliceOfficersStandingSpawned[i].BlockPermanentEvents = true;
                            PoliceOfficersStandingSpawned[i].Tasks.FollowNavigationMeshToPosition(PoliceOfficersStandingLocations[i], PoliceOfficersStandingHeadings[i], 2f);
                        }
                    }
                }

            }
            for (int i = 0; i < PoliceOfficersAimingSpawned.Count; i++)
            {
                if (PoliceOfficersAimingSpawned[i].Exists())
                {
                    if (PoliceOfficersAimingSpawned[i].IsAlive)
                    {
                        if (Vector3.Distance(PoliceOfficersAimingSpawned[i].Position, PoliceOfficersAimingLocations[i]) > 0.5f)
                        {
                            PoliceOfficersAimingSpawned[i].BlockPermanentEvents = true;
                            PoliceOfficersAimingSpawned[i].Tasks.FollowNavigationMeshToPosition(PoliceOfficersAimingLocations[i], PoliceOfficersAimingHeadings[i], 2f);
                        }
                        else
                        {

                            Vector3 AimPoint;
                            if (Vector3.Distance(PoliceOfficersAimingSpawned[i].Position, PacificBankDoors[0]) < Vector3.Distance(PoliceOfficersAimingSpawned[i].Position, PacificBankDoors[1]))
                            {
                                AimPoint = PacificBankDoors[0];
                            }
                            else
                            {
                                AimPoint = PacificBankDoors[1];
                            }
                            Rage.Native.NativeFunction.Natives.TASK_AIM_GUN_AT_COORD(PoliceOfficersAimingSpawned[i], AimPoint.X, AimPoint.Y, AimPoint.Z, -1, false, false);
                        }
                    }
                }
            }
        }
        //STATUS: refactored, review
        private void SneakyRobbersAI()
        {
            //TODO:
            // - extract behaviour to a dedicated sneaky robber class
            // OR
            // - add IsFighting bool as a data class member

            var validSneakyRobbers = SneakyRobbers.FindAll(r => r.Instance.Exists() && r.Instance.IsAlive && !SneakyRobbersFighting.Contains(r.Instance));

            foreach (var r in validSneakyRobbers)
            {
                if (Vector3.Distance(r.Instance.Position, r.Position) > 0.7f)
                {
                    r.Instance.Tasks.FollowNavigationMeshToPosition(r.Position, r.Heading, 2f).WaitForCompletion(300);
                }
                else
                {
                    if (!NativeFunction.Natives.IS_ENTITY_PLAYING_ANIM<bool>(r.Instance, "cover@weapon@rpg", "blindfire_low_l_enter_low_edge", 3))
                    {
                        r.Instance.Tasks.PlayAnimation("cover@weapon@rpg", "blindfire_low_l_enter_low_edge", 2f, AnimationFlags.StayInEndFrame).WaitForCompletion(20);
                    }
                }

                var nearestPeds = r.Instance.GetNearbyPeds(3).Where(p => p.Exists() && p.IsAlive && (p.RelationshipGroup == "PLAYER" || p.RelationshipGroup == "COP")).ToList();

                foreach (Ped nearestPed in nearestPeds)
                {
                    if (Vector3.Distance(nearestPed.Position, r.Instance.Position) < 3.9f)
                    {
                        if (Math.Abs(nearestPed.Position.Z - r.Instance.Position.Z) < 0.9f)
                        {
                            SneakyRobbersFighting.Add(r.Instance);
                            SneakyRobberFight(r.Instance, nearestPed);

                            //RobberAttackSituation.Add(new RobberAttackData() { Robber = r.Instance, AttackedPed = nearestPed });

                            break;
                        }
                    }
                }
            }
        }

        private List<Ped> SneakyRobbersFighting = new List<Ped>();
        //SneakyFighting: <Ped, Ped>: robber, nearestPed

        //private List<Tuple<Ped, Ped>>
        private List<RobberAttackData> RobberAttackSituation = new List<RobberAttackData>();

        private List<RobberAttackData> RobberAttackRobberFights = new List<RobberAttackData>();

        private class RobberAttackData
        {
            public Ped Robber;
            public Ped AttackedPed;
        }

        private Entity entityPlayerAimingAtSneakyRobber = null;
        //STATUS: refactored, review
        private void SneakyRobberCanInitFight_Process()
        {
            for (int i = RobberAttackSituation.Count - 1; i >= 0; i--)
            {
                var attack = RobberAttackSituation[i];
                if (!attack.Robber.Exists() || !attack.AttackedPed.Exists() || !attack.Robber.IsAlive || !attack.AttackedPed.IsAlive)
                {
                    RobberAttackSituation.RemoveAt(i);
                    continue;
                }

                var dist = Vector3.Distance(attack.AttackedPed.Position, attack.Robber.Position);
                //TODO: try w/o try..catch
                try { entityPlayerAimingAtSneakyRobber = GetEntityPlayerAimsAt(); } catch (Exception e) { };
                if (dist > 5.1f || dist < 1.7f || entityPlayerAimingAtSneakyRobber == attack.Robber || RescuingHostage)
                {
                    RobberAttackSituation.RemoveAt(i);
                    SneakyRobberFight(attack);
                    RobberAttackRobberFights.Add(attack);
                }
            }
        }
        //STATUS: refactored
        private void SneakyRobberFight(RobberAttackData r)
        {
            if (r.Robber.Exists())
            {
                r.Robber.Tasks.FightAgainstClosestHatedTarget(15f);
                r.Robber.RelationshipGroup = "ROBBERS";
            }
        }
        //STATUS: refactored, review
        private void SneakyRobberFightInProgress()
        {
            for (int i = RobberAttackRobberFights.Count - 1; i >= 0; i--)
            {
                var r = RobberAttackRobberFights[i];

                if (!r.Robber.Exists() || r.Robber.IsDead || !r.AttackedPed.Exists())
                {
                    continue;
                }

                Rage.Native.NativeFunction.Natives.STOP_CURRENT_PLAYING_AMBIENT_SPEECH(r.Robber);

                if (!r.AttackedPed.IsDead) continue;

                foreach (var hostage in Hostages)
                {
                    if (Math.Abs(hostage.Position.Z - r.Robber.Position.Z) < 0.6f)
                    {
                        if (Vector3.Distance(hostage.Position, r.Robber.Position) < 14f)
                        {
                            int waitCount = 0;
                            while (hostage.Instance.IsAlive)
                            {
                                GameFiber.Yield();
                                waitCount++;
                                if (waitCount > 450)
                                {
                                    hostage.Instance.Kill();
                                }
                            }

                            break;
                        }
                    }
                }
            }

            RobberAttackRobberFights.Clear();
        }
        //STATUS: old function, obsolete
        [Obsolete("Replace with specific f()")]
        private void SneakyRobberFight(Ped sneakyrobber, Ped nearestPed)
        {
        //TODO: WARNING! HARDCORE SOLUTIONS
            //TODO:
            // - process those in SneakyRobbersFighting list?

            GameFiber.StartNew(delegate
            {
                try
                {

                    //loop keeps robber inactive until dist is between min and max OR player aims at robber
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();

                        if (!nearestPed.Exists()) { break; }
                        if (!sneakyrobber.Exists()) { break; }
                        if (!sneakyrobber.IsAlive) { break; }
                        if (!nearestPed.IsAlive) { break; }

                        if (Vector3.Distance(nearestPed.Position, sneakyrobber.Position) > 5.1f)
                        {
                            break;
                        }
                        else if (Vector3.Distance(nearestPed.Position, sneakyrobber.Position) < 1.70f)
                        {
                            break;
                        }

                        try
                        {
                            entityPlayerAimingAtSneakyRobber = GetEntityPlayerAimsAt();
                        }
                        catch (Exception e)
                        {
                        }

                        if (entityPlayerAimingAtSneakyRobber == sneakyrobber)
                        {
                            break;
                        }
                        if (RescuingHostage) { break; }
                    }


                    if (sneakyrobber.Exists())
                    {
                        sneakyrobber.Tasks.FightAgainstClosestHatedTarget(15f);
                        sneakyrobber.RelationshipGroup = "ROBBERS";
                    }

                    while (CalloutRunning)
                    {
                        GameFiber.Yield();

                        if (!sneakyrobber.Exists()) { break; }
                        if (!nearestPed.Exists()) { break; }

                        Rage.Native.NativeFunction.Natives.STOP_CURRENT_PLAYING_AMBIENT_SPEECH(sneakyrobber);

                        if (nearestPed.IsDead)
                        {
                            foreach (Ped hostage in SpawnedHostages)
                            {
                                if (Math.Abs(hostage.Position.Z - sneakyrobber.Position.Z) < 0.6f)
                                {
                                    if (Vector3.Distance(hostage.Position, sneakyrobber.Position) < 14f)
                                    {
                                        int waitCount = 0;
                                        while (hostage.IsAlive)
                                        {
                                            GameFiber.Yield();
                                            waitCount++;
                                            if (waitCount > 450)
                                            {
                                                hostage.Kill();
                                            }
                                        }

                                        break;
                                    }
                                }
                            }
                            break;
                        }

                        if (sneakyrobber.IsDead)
                        {
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                }
                finally
                {
                    SneakyRobbersFighting.Remove(sneakyrobber);
                }
            });
        }

        private const float DIST_ACTIVATE_VAULT_ROBBERS = 4f;
        //STATUS: refactored, review
        private void HandleVaultRobbers()
        {
            if (DistToPlayer(OutsideBankVault) < DIST_ACTIVATE_VAULT_ROBBERS)
            {
                GameFiber.Wait(2000);

                VaultRobbers[2].Instance.Tasks.FollowNavigationMeshToPosition(OutsideBankVault, VaultRobbers[2].Instance.Heading, 2f).WaitForCompletion(500);

                World.SpawnExplosion(new Vector3(252.2609f, 225.3824f, 101.6835f), 2, 0.2f, true, false, 0.6f);

                CurrentAudioState = AudioState.Alarm;
                AudioStateChanged = true;

                GameFiber.Wait(900);

                VaultRobbers.ForEach(r => r.Instance.Tasks.FightAgainstClosestHatedTarget(23f));

                GameFiber.Wait(3000);

                VaultRobbers.ForEach(r => Robbers.Add(r.Instance));

                proc.DeactivateProcess(HandleVaultRobbers);
            }
        }
                       
        //HOSTAGES=======================================================================================


        private Ped closeHostage;
        private int subtitleCount;
        //STATUS: refactored, review
        private void HandleEndangeredHostages()
        {
            for (int i = Hostages.Count - 1; i >= 0; i--)
            {
                var hostage = Hostages[i].Instance;

                if (!hostage.Exists() || !hostage.IsAlive)
                {
                    Hostages.RemoveAt(i);
                    AliveHostagesCount--;
                    continue;
                }

                if (Functions.IsPedGettingArrested(hostage) || Functions.IsPedArrested(hostage))
                {
                    Hostages[i].Instance = hostage.ClonePed();
                }

                hostage.Tasks.PlayAnimation("random@arrests", "kneeling_arrest_idle", 1f, AnimationFlags.Loop);

                if (Player.IsShooting) continue;
                
                if (DistToPlayer(hostage.Position) < 1.45f)
                {
                    if (ExtensionMethods.IsKeyDownRightNowComputerCheck(AssortedCalloutsHandler.HostageRescueKey))
                    {
                        Vector3 directionFromPlayerToHostage = (hostage.Position - Game.LocalPlayer.Character.Position);
                        directionFromPlayerToHostage.Normalize();
                        RescuingHostage = true;

                        Player.Tasks.AchieveHeading(MathHelper.ConvertDirectionToHeading(directionFromPlayerToHostage)).WaitForCompletion(1200);
                        hostage.RelationshipGroup = "COP";

                        SpeechHandler.HandleBankHeistSpeech(new List<string>() { "You: Come on! It's safe, get to the ambulance outside!" }, WaitAfterLastLine: false);
                        Player.Tasks.PlayAnimation("random@rescue_hostage", "bystander_helping_girl_loop", 1.5f, AnimationFlags.None).WaitForCompletion(3000);

                        if (hostage.IsAlive)
                        {
                            hostage.Tasks.PlayAnimation("random@arrests", "kneeling_arrest_get_up", 0.9f, AnimationFlags.None).WaitForCompletion(6000);
                            Player.Tasks.ClearImmediately();

                            hostage.Tasks.FollowNavigationMeshToPosition(HostageSafeLocation, HostageSafeHeading, 1.55f);

                            RescuedHostages.Add(Hostages[i]);
                            Hostages.RemoveAt(i);
                        }
                        else
                        {
                            Player.Tasks.ClearImmediately();
                        }

                        RescuingHostage = false;
                    }
                    else
                    {
                        subtitleCount++;
                        closeHostage = hostage;

                        if (subtitleCount > 10)
                        {
                            Game.DisplaySubtitle("~s~Hold ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.HostageRescueKey) + " ~s~to release the hostage.", 500);
                        }
                    }
                }
                else
                {
                    if (hostage == closeHostage)
                    {
                        subtitleCount = 0;
                    }
                }
            }
        }

        int waitCountForceAttack = 0;
        //STATUS: refactored, review
        private void HandleRescuedHostages()
        {
            if (waitCountForceAttack > 250)
            {
                waitCountForceAttack = 0;
            }

            for (int i = RescuedHostages.Count - 1; i >= 0; i--)
            {
                var rescuedHostage = RescuedHostages[i].Instance;
                
                if (rescuedHostage.Exists() && rescuedHostage.IsAlive)
                {
                    if (Hostages.Any(h => h.Instance == rescuedHostage))
                    {
                        Hostages.RemoveAll(h => h.Instance == rescuedHostage);
                    }

                    if (Vector3.Distance(rescuedHostage.Position, HostageSafeLocation) < 3f)
                    {
                        SafeHostages.Add(RescuedHostages[i]);
                        SafeHostagesCount++;
                    }

                    if (Functions.IsPedGettingArrested(rescuedHostage) || Functions.IsPedArrested(rescuedHostage))
                    {
                        RescuedHostages[i].Instance = rescuedHostage.ClonePed();
                    }

                    rescuedHostage.Tasks.FollowNavigationMeshToPosition(HostageSafeLocation, HostageSafeHeading, 1.55f).WaitForCompletion(200);

                    if (waitCountForceAttack > 150)
                    {
                        Ped nearestPed = rescuedHostage.GetNearbyPeds(2)[0];
                        if (nearestPed == Game.LocalPlayer.Character)
                        {
                            nearestPed = rescuedHostage.GetNearbyPeds(2)[1];
                        }

                        if (Robbers.Contains(nearestPed))
                        {
                            nearestPed.Tasks.FightAgainst(rescuedHostage);
                            waitCountForceAttack = 0;
                        }
                    }
                }
                else
                {
                    RescuedHostages.RemoveAt(i);
                    AliveHostagesCount--;
                }
            }
        }

        int deleteSafeHostageCount = 0;
        int enterAmbulanceCount = 0;
        //STATUS: refactored, review
        private void HandleSafeHostages()
        {
            if (enterAmbulanceCount > 101)
            {
                enterAmbulanceCount = 101;
            }

            for (int i = SafeHostages.Count - 1; i >= 0; i--)
            {
                var safeHostage = SafeHostages[i].Instance;

                if (!safeHostage.Exists())
                {
                    SafeHostages.RemoveAt(i);
                    continue;
                }

                if (RescuedHostages.Contains(SafeHostages[i]))
                {
                    RescuedHostages.Remove(SafeHostages[i]);
                }

                safeHostage.IsInvincible = true;

                if (!safeHostage.IsInAnyVehicle(true))
                {
                    if (enterAmbulanceCount > 100)
                    {
                        var ambo = Ambulances[1].Instance;

                        if (ambo.IsSeatFree(2))
                        {
                            safeHostage.Tasks.EnterVehicle(ambo, 2);
                        }
                        else if (ambo.IsSeatFree(1))
                        {
                            safeHostage.Tasks.EnterVehicle(ambo, 1);
                        }
                        else
                        {
                            ambo.GetPedOnSeat(2).Delete();
                            safeHostage.Tasks.EnterVehicle(ambo, 2);
                        }

                        enterAmbulanceCount = 0;
                    }
                }
                else
                {
                    deleteSafeHostageCount++;
                    if (deleteSafeHostageCount > 50)
                    {
                        if (DistToPlayer(safeHostage.Position) > 22f)
                        {
                            if (safeHostage.IsInAnyVehicle(false))
                            {
                                safeHostage.Delete();

                                deleteSafeHostageCount = 0;
                                Rage.Native.NativeFunction.Natives.SET_VEHICLE_DOORS_SHUT(Ambulances[1].Instance, true);
                            }
                        }
                    }
                }
            }
        }

        private bool RescuingHostage = false;
        //STATUS: old f()
        [Obsolete("Replaced by refactored functions")]
        private void HandleHostages()
        {
            Game.FrameRender += DrawHostageCount;

            GameFiber.StartNew(delegate
            {
                int waitCountForceAttack = 0;
                int enterAmbulanceCount = 0;
                int deleteSafeHostageCount = 0;
                int subtitleCount = 0;
                Ped closeHostage = null;
                while (CalloutRunning)
                {
                    try
                    {
                        waitCountForceAttack++;
                        enterAmbulanceCount++;

                        GameFiber.Yield();
                        if (waitCountForceAttack > 250)
                        {
                            waitCountForceAttack = 0;
                        }
                        if (enterAmbulanceCount > 101)
                        {
                            enterAmbulanceCount = 101;
                        }

                        //TODO: use dedicated hostage class
                        foreach (var h in Hostages)
                        {
                            var hostage = h.Instance;

                            GameFiber.Yield();
                            if (hostage.Exists())
                            {
                                if (hostage.IsAlive)
                                {
                                    if (Functions.IsPedGettingArrested(hostage) || Functions.IsPedArrested(hostage))
                                    {
                                        SpawnedHostages[SpawnedHostages.IndexOf(hostage)] = hostage.ClonePed();
                                    }
                                    hostage.Tasks.PlayAnimation("random@arrests", "kneeling_arrest_idle", 1f, AnimationFlags.Loop);
                                    if (!Game.LocalPlayer.Character.IsShooting)
                                    {
                                        if (DistToPlayer(hostage.Position) < 1.45f)
                                        {
                                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(AssortedCalloutsHandler.HostageRescueKey))
                                            {
                                                Vector3 directionFromPlayerToHostage = (hostage.Position - Game.LocalPlayer.Character.Position);
                                                directionFromPlayerToHostage.Normalize();
                                                RescuingHostage = true;
                                                Game.LocalPlayer.Character.Tasks.AchieveHeading(MathHelper.ConvertDirectionToHeading(directionFromPlayerToHostage)).WaitForCompletion(1200);
                                                hostage.RelationshipGroup = "COP";
                                                SpeechHandler.HandleBankHeistSpeech(new List<string>() { "You: Come on! It's safe, get to the ambulance outside!" }, WaitAfterLastLine: false);
                                                Game.LocalPlayer.Character.Tasks.PlayAnimation("random@rescue_hostage", "bystander_helping_girl_loop", 1.5f, AnimationFlags.None).WaitForCompletion(3000);

                                                if (hostage.IsAlive)
                                                {
                                                    hostage.Tasks.PlayAnimation("random@arrests", "kneeling_arrest_get_up", 0.9f, AnimationFlags.None).WaitForCompletion(6000);
                                                    Game.LocalPlayer.Character.Tasks.ClearImmediately();

                                                    if (hostage.IsAlive)
                                                    {
                                                        hostage.Tasks.FollowNavigationMeshToPosition(HostageSafeLocation, HostageSafeHeading, 1.55f);

                                                        RescuedHostages.Add(hostage);
                                                        SpawnedHostages.Remove(hostage);
                                                    }
                                                    else
                                                    {
                                                        Game.LocalPlayer.Character.Tasks.ClearImmediately();
                                                    }
                                                }
                                                else
                                                {
                                                    Game.LocalPlayer.Character.Tasks.ClearImmediately();
                                                }
                                                RescuingHostage = false;
                                            }
                                            else
                                            {
                                                subtitleCount++;
                                                closeHostage = hostage;

                                                if (subtitleCount > 10)
                                                {
                                                    Game.DisplaySubtitle("~s~Hold ~b~" + AssortedCalloutsHandler.kc.ConvertToString(AssortedCalloutsHandler.HostageRescueKey) + " ~s~to release the hostage.", 500);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (hostage == closeHostage)
                                            {
                                                subtitleCount = 0;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    SpawnedHostages.Remove(hostage);
                                    AliveHostagesCount--;
                                }
                            }
                            else
                            {
                                SpawnedHostages.Remove(hostage);
                                AliveHostagesCount--;
                            }
                        }


                        foreach (Ped rescuedHostage in RescuedHostages)
                        {
                            if (rescuedHostage.Exists() && rescuedHostage.IsAlive)
                            {
                                if (SpawnedHostages.Contains(rescuedHostage))
                                {
                                    SpawnedHostages.Remove(rescuedHostage);
                                }
                                if (Vector3.Distance(rescuedHostage.Position, HostageSafeLocation) < 3f)
                                {
                                    SafeHostages.Add(rescuedHostage);
                                    SafeHostagesCount++;
                                }
                                if (Functions.IsPedGettingArrested(rescuedHostage) || Functions.IsPedArrested(rescuedHostage))
                                {
                                    RescuedHostages[RescuedHostages.IndexOf(rescuedHostage)] = rescuedHostage.ClonePed();
                                }
                                rescuedHostage.Tasks.FollowNavigationMeshToPosition(HostageSafeLocation, HostageSafeHeading, 1.55f).WaitForCompletion(200);

                                if (waitCountForceAttack > 150)
                                {
                                    Ped nearestPed = rescuedHostage.GetNearbyPeds(2)[0];
                                    if (nearestPed == Game.LocalPlayer.Character)
                                    {
                                        nearestPed = rescuedHostage.GetNearbyPeds(2)[1];
                                    }
                                    if (Robbers.Contains(nearestPed))
                                    {

                                        nearestPed.Tasks.FightAgainst(rescuedHostage);
                                        waitCountForceAttack = 0;

                                    }
                                }
                            }
                            else
                            {
                                RescuedHostages.Remove(rescuedHostage);
                                AliveHostagesCount--;
                            }
                        }

                        foreach (Ped safeHostage in SafeHostages)
                        {
                            if(!safeHostage.Exists())
                            {
                                SafeHostages.Remove(safeHostage);
                                continue;
                            }

                            if (RescuedHostages.Contains(safeHostage))
                            {
                                RescuedHostages.Remove(safeHostage);
                            }

                            safeHostage.IsInvincible = true;

                            if (!safeHostage.IsInAnyVehicle(true))
                            {
                                if (enterAmbulanceCount > 100)
                                {
                                    var ambo = Ambulances[1].Instance;

                                    if (ambo.IsSeatFree(2))
                                    {
                                        safeHostage.Tasks.EnterVehicle(ambo, 2);
                                    }
                                    else if (ambo.IsSeatFree(1))
                                    {
                                        safeHostage.Tasks.EnterVehicle(ambo, 1);
                                    }
                                    else
                                    {
                                        ambo.GetPedOnSeat(2).Delete();
                                        safeHostage.Tasks.EnterVehicle(ambo, 2);
                                    }

                                    enterAmbulanceCount = 0;
                                }
                            }
                            else
                            {
                                deleteSafeHostageCount++;
                                if (deleteSafeHostageCount > 50)
                                {
                                    if (DistToPlayer(safeHostage.Position) > 22f)
                                    {
                                        if (safeHostage.IsInAnyVehicle(false))
                                        {
                                            safeHostage.Delete();

                                            deleteSafeHostageCount = 0;
                                            Rage.Native.NativeFunction.Natives.SET_VEHICLE_DOORS_SHUT(Ambulances[1].Instance, true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e) { continue; }
                }
            });
        }
        //STATUS: review
        private void DrawHostageCount(object sender, GraphicsEventArgs e)
        {
            if (fighting || (SurrenderComplete && TalkedToWells2nd))
            {
                e.Graphics.DrawText("Hostages Rescued: " + SafeHostagesCount.ToString() + "/" + AliveHostagesCount.ToString(), "Aharoni Bold", 20.0f, new PointF(1, 6), Color.LightBlue);
                if (TotalHostagesCount - AliveHostagesCount > 0)
                {
                    e.Graphics.DrawText("Hostages Killed: " + (TotalHostagesCount - AliveHostagesCount).ToString(), "Aharoni Bold", 20.0f, new PointF(1, 30), Color.Red);
                }
            }
            if (!CalloutRunning || DoneFighting)
            {
                Game.FrameRender -= DrawHostageCount;
            }
        }
        //STATUS: review
        private void HandleAudio()
        {
            //TODO: refactor CurrentAudioState - looks like some of vars used here can be removed?
            if (!HandlingRespawn)
            {
                if (!AlarmPlaying && DistToPlayer(SpawnPoint) < DIST_PLAY_BANK_ALARM_ON)
                {
                    AlarmPlaying = true;
                    CurrentAudioState = AudioState.Alarm;
                    SuspectBlip.IsRouteEnabled = false;
                    AudioStateChanged = true;
                }
                else if (DistToPlayer(SpawnPoint) > DIST_PLAY_BANK_ALARM_OFF)
                {
                    AlarmPlaying = false;
                    CurrentAudioState = AudioState.None;
                    SuspectBlip.IsRouteEnabled = true;
                    AudioStateChanged = true;
                }

                if (ExtensionMethods.IsKeyDownComputerCheck(AssortedCalloutsHandler.ToggleAlarmKey))
                {
                    if (CurrentAudioState != AudioState.None)
                    {
                        CurrentAudioState += 1;
                    }
                    else
                    {
                        CurrentAudioState = AudioState.Alarm;
                    }

                    AudioStateChanged = true;
                }

                if (AudioStateChanged)
                {
                    switch (CurrentAudioState)
                    {
                        case AudioState.Alarm:
                            AlarmPlayer.PlayLooping();
                            break;

                        case AudioState.None:
                            AlarmPlayer.Stop();
                            break;
                    }

                    AudioStateChanged = false;
                }
            }
        }

        private int RiotVan_CoolDown = 0;
        //STATUS: refactored, review
        private void HandleOpenBackRiotVan()
        {
            if (RiotVan_CoolDown > 0) { RiotVan_CoolDown--; }
            if (HandlingRespawn) { RiotVan_CoolDown = 0; }

            RiotVans.ForEach(v => RiotVanEQHandler(v, RiotVan_CoolDown));
        }
        //STATUS: refactored, review
        private void RiotVanEQHandler(Vehicle riotVan, int coolDown)
        {
            if (DistToPlayer(riotVan.GetOffsetPosition(Vector3.RelativeBack * 4f)) < 2f)
            {
                if (ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.Enter))
                {
                    if (coolDown > 0)
                    {
                        Game.DisplayNotification("The gear has temporarily run out.");
                    }
                    else
                    {
                        coolDown = 3500;
                        Player.Tasks.EnterVehicle(RiotVans[0], 1).WaitForCompletion();
                        Player.Armor = 100;
                        Player.Health = Player.MaxHealth;
                        Player.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_CARBINERIFLE"), 150, true);

                        Player.Inventory.GiveNewWeapon(new WeaponAsset(Grenades[1]), 3, false);
                        Rage.Native.NativeFunction.Natives.PLAY_SOUND_FRONTEND(-1, "PURCHASE", "HUD_LIQUOR_STORE_SOUNDSET", 1);
                        Player.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();
                        FightingPacksUsed++;
                    }
                }
                else
                {
                    if (coolDown == 0)
                    {
                        Game.DisplaySubtitle("~h~Press ~b~Enter ~s~to retrieve gear from the van.", 500);
                    }
                }
            }
        }

        private Entity entityPlayerAimingAt;
        //STATUS: refactor, extract specialized f()
        [Obsolete("Refactor, use eg. HandleMinigunRobberBehavior")]
        private void RobbersFightingAI()
        {
            GameFiber.StartNew(delegate
            {
                while (CalloutRunning)
                {
                    try
                    {
                        GameFiber.Yield();

                        if (fighting)
                        {
                            foreach (Ped robber in Robbers)
                            {
                                GameFiber.Yield();

                                if (!robber.Exists()) continue; //TODO: remove from list

                                float distCheck0 = Vector3.Distance(robber.Position, PacificBankInsideChecks[0]);
                                float distCheck1 = Vector3.Distance(robber.Position, PacificBankInsideChecks[1]);

                                float Distance = Math.Min(distCheck0, distCheck1);

                                if (Distance < 16.5f) Distance = 16.5f;
                                else if (Distance > 21f) Distance = 21f;

                                robber.RegisterHatedTargetsAroundPed(Distance);
                                robber.Tasks.FightAgainstClosestHatedTarget(Distance);
                            }

                            if (MiniGunRobber.Exists())
                            {
                                var distPlayerToMinigun = DistToPlayer(MiniGunFireLocation);
                                var distPlayerGlassDoor = DistToPlayer(BehindGlassDoorLocation);


                                if (distPlayerToMinigun < distPlayerGlassDoor)
                                {
                                    if (distPlayerToMinigun < 4.7f)
                                    {
                                        MiniGunRobberFiring = true;

                                    }
                                    else if (distPlayerToMinigun > 12f)
                                    {
                                        MiniGunRobberFiring = false;
                                    }
                                    //TODO: what if 4.7 < d < 12 => keeps shootin' or stays low
                                }
                                else
                                {
                                    if (distPlayerGlassDoor < 2.1f)
                                    {
                                        MiniGunRobberFiring = true;
                                    }
                                    else if (distPlayerGlassDoor > 6f)
                                    {
                                        MiniGunRobberFiring = false;
                                    }
                                }

                                if (MiniGunRobberFiring)
                                {

                                    Rage.Native.NativeFunction.Natives.TASK_SHOOT_AT_ENTITY(MiniGunRobber, Game.LocalPlayer.Character, 2700, Game.GetHashKey("FIRING_PATTERN_FULL_AUTO"));
                                }
                                else
                                {
                                    MiniGunRobber.Tasks.FollowNavigationMeshToPosition(MiniGunRobberLocation, MiniGunRobberHeading, 2f);

                                }
                            }

                            try
                            {
                                entityPlayerAimingAt = GetEntityPlayerAimsAt();
                            }
                            catch (Exception e) { Game.LogTrivial(e.ToString()); }

                            if (Robbers.Contains(entityPlayerAimingAt))
                            {

                                Ped pedAimingAt = (Ped)entityPlayerAimingAt;
                                pedAimingAt.Tasks.FightAgainst(Game.LocalPlayer.Character);
                            }

                            GameFiber.Sleep(3000);
                        }
                    }
                    catch (Exception e)
                    {
                        Game.LogTrivial(e.ToString());
                    }
                }
            });

        }
        //STATUS: refactored, review
        private void HandleRobberMinigunBehavior()
        {
            if (!MiniGunRobber.Exists()) return; //TODO: disable proc

            var distPlayerToMinigun = DistToPlayer(MiniGunFireLocation);
            var distPlayerGlassDoor = DistToPlayer(BehindGlassDoorLocation);

            if (distPlayerToMinigun < distPlayerGlassDoor)
            {
                if (distPlayerToMinigun < 4.7f)
                {
                    MiniGunRobberFiring = true;

                }
                else if (distPlayerToMinigun > 12f)
                {
                    MiniGunRobberFiring = false;
                }
            }
            else
            {
                if (distPlayerGlassDoor < 2.1f)
                {
                    MiniGunRobberFiring = true;
                }
                else if (distPlayerGlassDoor > 6f)
                {
                    MiniGunRobberFiring = false;
                }
            }

            if (MiniGunRobberFiring)
            {

                Rage.Native.NativeFunction.Natives.TASK_SHOOT_AT_ENTITY(MiniGunRobber, Game.LocalPlayer.Character, 2700, Game.GetHashKey("FIRING_PATTERN_FULL_AUTO"));
            }
            else
            {
                MiniGunRobber.Tasks.FollowNavigationMeshToPosition(MiniGunRobberLocation, MiniGunRobberHeading, 2f);

            }
        }
        //STATUS: review, add try..catch and return null in case of an exception
        private Entity GetEntityPlayerAimsAt()
        {
            unsafe
            {
                uint entityHandle;
                NativeFunction.Natives.x2975C866E6713290(Game.LocalPlayer, new IntPtr(&entityHandle)); // Stores the entity the player is aiming at in the uint provided in the second parameter.

                return World.GetEntityByHandle<Entity>(entityHandle);
            }
        }

        private GameFiber CopFightingAIGameFiber;
        //STATUS: to be refactored
        private void CopFightingAI()
        {
            CopFightingAIGameFiber = GameFiber.StartNew(delegate
            {
                while (CalloutRunning)
                {
                    GameFiber.Yield();

                    if (fighting)
                    {
                        if (PoliceOfficersTargetsToShoot.Count > 0)
                        {
                            var target = PoliceOfficersTargetsToShoot[0];

                            if (target.Exists() && target.IsAlive)
                            {
                                PoliceOfficersSpawned.ForEach(p => p.Tasks.FightAgainst(target));
                            }
                            else
                            {
                                PoliceOfficersTargetsToShoot.RemoveAt(0);
                            }
                        }
                        else
                        {
                            CopsReturnToLocation();
                        }
                    }

                    if (fighting || SWATFollowing)
                    {
                        foreach (var op in SWATOperators)
                        {
                            if (!op.Instance.Exists()) continue;

                            if(!SWATFollowing)
                            {
                                op.Instance.RegisterHatedTargetsAroundPed(60f);
                                op.Instance.Tasks.FightAgainstClosestHatedTarget(60f);
                            }
                            else
                            {
                                op.Instance.Tasks.FollowNavigationMeshToPosition(PlayerPosition, Player.Heading, 1.6f, GetDistTresholdForCops(op.Instance));
                            }
                        }

                        GameFiber.Sleep(4000);
                    }
                }
            });
        }
        //STATUS: done
        private float GetDistTresholdForCops(Ped cop)
        {
            return Math.Abs(PlayerPosition.Z - cop.Position.Z) > 1f ? 1f : 4f;
        }
        //STATUS: done
        private void SetRelationshipGroups(Relationship relPlayerCopHostage)
        {
            Game.SetRelationshipBetweenRelationshipGroups("COP", "ROBBERS", Relationship.Hate);
            Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "COP", Relationship.Hate);
            Game.SetRelationshipBetweenRelationshipGroups("ROBBERS", "PLAYER", Relationship.Hate);
            Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "ROBBERS", Relationship.Hate);
            Game.SetRelationshipBetweenRelationshipGroups("COP", "PLAYER", relPlayerCopHostage);
            Game.SetRelationshipBetweenRelationshipGroups("PLAYER", "COP", relPlayerCopHostage);
            Game.SetRelationshipBetweenRelationshipGroups("HOSTAGE", "PLAYER", relPlayerCopHostage);
            Game.SetRelationshipBetweenRelationshipGroups("SNEAKYROBBERS", "PLAYER", Relationship.Hate);
        }
        //STATUS: done
        private void SetWeaponModifiersForPlayer(float damage)
        {
            Rage.Native.NativeFunction.Natives.SET_PLAYER_WEAPON_DEFENSE_MODIFIER(Game.LocalPlayer, 0.45f);
            Rage.Native.NativeFunction.Natives.SET_PLAYER_WEAPON_DAMAGE_MODIFIER(Game.LocalPlayer, damage);
            Rage.Native.NativeFunction.Natives.SET_AI_MELEE_WEAPON_DAMAGE_MODIFIER(1f);
        }
        //STATUS: done
        private void DoorControl()
        {
            //TODO: add a struct to hold DoorInformation
            NativeFunction.CallByHash<uint>(_DOOR_CONTROL, 4072696575, 256.3116f, 220.6579f, 106.4296f, false, 0f, 0f, 0f);
            NativeFunction.CallByHash<uint>(_DOOR_CONTROL, 746855201, 262.1981f, 222.5188f, 106.4296f, false, 0f, 0f, 0f);
            NativeFunction.CallByHash<uint>(_DOOR_CONTROL, 110411286, 258.2022f, 204.1005f, 106.4049f, false, 0f, 0f, 0f);
        }
        //STATUS: research
        private void FinalDialogue()
        {
            SpeechHandler.CptWellsLineAudioCount = 21;
            SpeechHandler.YouLineAudioCount = 10;
            SpeechHandler.HandleBankHeistSpeech(FinalDialogueText, LineFolderModifier: "Outro");
        }
        //STATUS: to be refactored
        private void DetermineResults()
        {
            int HostagesDead = TotalHostagesCount - AliveHostagesCount;

            RobbersKilled += Robbers.Count(r => r.Exists() && r.IsDead);

            RobbersKilled += SneakyRobbers.Count(r => r.Instance.Exists() && r.Instance.IsDead);

            RobbersKilled += (MiniGunRobber.Exists() && MiniGunRobber.IsDead) ? 1 : 0;

            SWATUnitsdied += SWATOperators.Count(s => s.Instance.Exists() && s.Instance.IsDead);

            Game.DisplayNotification("mphud", "mp_player_ready", "~h~Captain Wells", "Operation Report", "Hostages Rescued: " + SafeHostagesCount.ToString() + "~n~Hostages Dead: " + HostagesDead.ToString() + "~n~Robbers Killed: " + RobbersKilled.ToString() + "~n~Robbers Surrendered: " + SurrenderComplete.ToString());
            Game.DisplayNotification("mphud", "mp_player_ready", "~h~Captain Wells", "Operation Report - Continued", "Times died: " + TimesDied.ToString() + "~n~Times gear resupplied: " + FightingPacksUsed.ToString() + "~n~SWAT units died: " + SWATUnitsdied.ToString() + "~n~~b~End of report.");

            if (HostagesDead == 0)
            {
                BigMessageThread bigMessage = new BigMessageThread(true);
                bigMessage.MessageInstance.ShowMissionPassedMessage("All hostages were saved! Great job!", time: 8000);
            }
        }
        //STATUS: refactored, review, use const dist
        private void MakeNearbyPedsFlee()
        {
            var selectedPeds = World.GetEntities(SpawnPoint, 80f, GetEntitiesFlags.ConsiderAllPeds | GetEntitiesFlags.ExcludePlayerPed | GetEntitiesFlags.ExcludePoliceOfficers);

            foreach (Ped entity in selectedPeds)
            {
                if (AllBankHeistEntities.Contains(entity))
                {
                    continue;
                }

                if (!entity.Exists() || !entity.IsValid() || entity == Player || entity == Player.CurrentVehicle || entity.CreatedByTheCallingPlugin)
                {
                    continue;
                }

                if (Vector3.Distance(entity.Position, SpawnPoint) < 74f)
                {
                    if (entity.IsInAnyVehicle(false))
                    {
                        if (entity.CurrentVehicle.Exists())
                        {
                            entity.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                        }
                    }
                    else
                    {
                        NativeFunction.CallByName<uint>("TASK_SMART_FLEE_COORD", entity, SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z, 75f, 6000, true, true);
                    }
                }

                if (Vector3.Distance(entity.Position, SpawnPoint) < 65f)
                {
                    DeleteEntity(entity.CurrentVehicle);
                    DeleteEntity(entity);
                }
            }
        }
        [Obsolete("Old f(), replaced with proc")]
        private void CreateSpeedZone()
        {
            GameFiber.StartNew(delegate
            {
                while (CalloutRunning)
                {
                    GameFiber.Yield();

                    foreach (Vehicle veh in World.GetEntities(SpawnPoint, 75f, GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ExcludePoliceCars | GetEntitiesFlags.ExcludeFiretrucks | GetEntitiesFlags.ExcludeAmbulances))
                    {
                        GameFiber.Yield();

                        if (AllBankHeistEntities.Contains(veh))
                        {
                            continue;
                        }

                        if(!veh.Exists() || veh == Player.CurrentVehicle || veh.CreatedByTheCallingPlugin)
                        {
                            continue;
                        }

                        if (veh.Velocity.Length() > 0f)
                        {
                            Vector3 velocity = veh.Velocity;
                            velocity.Normalize();
                            velocity *= 0f;
                            veh.Velocity = velocity;
                        }
                    }
                }
            });
        }
        //STATUS: refactored
        private void SpawnHostages()
        {
            for (int i = 0; i < Hostages.Count; i++)
            {
                var h = Hostages[i];

                var model = new Model(HostageModels[AssortedCalloutsHandler.rnd.Next(HostageModels.Length)]);
                Ped hostage = new Ped(model, h.Position, h.Heading);
                h.Instance = hostage;

                hostage.IsPersistent = true;
                hostage.BlockPermanentEvents = true;
                Rage.Native.NativeFunction.Natives.SET_PED_CAN_RAGDOLL(hostage, false);
                hostage.RelationshipGroup = "HOSTAGE";
                hostage.CanAttackFriendlies = false;
                hostage.Tasks.PlayAnimation("random@arrests", "kneeling_arrest_idle", 1f, AnimationFlags.Loop);
                hostage.Armor = 0;
                hostage.Health = 100;

                AliveHostagesCount++;
                TotalHostagesCount++;
                AllBankHeistEntities.Add(hostage);
            }
        }
        //STATUS: refactored, replace foreach w/ for?
        private void SpawnEMSAndFire()
        {
            foreach (var a in Ambulances)
            {
                Vehicle ambulance = new Vehicle(new Model("AMBULANCE"), a.Position, a.Heading);
                a.Instance = ambulance;
                ambulance.IsPersistent = true;
                ambulance.IsSirenOn = true;
                ambulance.IsSirenSilent = true;
                AllBankHeistEntities.Add(ambulance);
            }

            foreach (var p in Paramedics)
            {
                Ped para = new Ped(new Model("S_M_M_PARAMEDIC_01"), p.Position, p.Heading);
                p.Instance = para;
                para.IsPersistent = true;
                para.BlockPermanentEvents = true;

                AllBankHeistEntities.Add(para);
            }
            foreach (var t in Firetrucks)
            {
                Vehicle firetruck = new Vehicle(new Model("FIRETRUK"), t.Position, t.Heading);
                t.Instance = firetruck;
                firetruck.IsPersistent = true;
                firetruck.IsSirenOn = true;
                firetruck.IsSirenSilent = true;

                AllBankHeistEntities.Add(firetruck);

                Ped fireman = new Ped(new Model("S_M_Y_FIREMAN_01"), SpawnPoint, 0f);
                fireman.WarpIntoVehicle(firetruck, -1);
                fireman.BlockPermanentEvents = true;
                fireman.IsPersistent = true;

                AllBankHeistEntities.Add(fireman);
            }
        }
        //STATUS: to be refactored, create proper data structs
        private void SpawnAllPoliceOfficers()
        {
            for (int i = 0; i < PoliceOfficersStandingLocations.Count; i++)
            {
                Ped officer = new Ped(new Model(LSPDModels[AssortedCalloutsHandler.rnd.Next(LSPDModels.Length)]), PoliceOfficersStandingLocations[i], PoliceOfficersStandingHeadings[i]);

                Functions.SetPedAsCop(officer);
                Functions.SetCopAsBusy(officer, true);
                officer.CanBeTargetted = false;
                officer.IsPersistent = true;
                officer.BlockPermanentEvents = true;
                officer.Inventory.GiveNewWeapon("WEAPON_PISTOL50", 10000, true);
                officer.RelationshipGroup = "COP";
                PoliceOfficersStandingSpawned.Add(officer);
                PoliceOfficersSpawned.Add(officer);
                AllBankHeistEntities.Add(officer);
                officer.CanAttackFriendlies = false;
            }

            for (int i = 0; i < PoliceOfficersAimingLocations.Count; i++)
            {
                Ped officer = new Ped(new Model(LSPDModels[AssortedCalloutsHandler.rnd.Next(LSPDModels.Length)]), PoliceOfficersAimingLocations[i], PoliceOfficersAimingHeadings[i]);
                Functions.SetPedAsCop(officer);
                Functions.SetCopAsBusy(officer, true);
                officer.IsPersistent = true;
                officer.CanBeTargetted = false;
                officer.BlockPermanentEvents = true;
                officer.Inventory.GiveNewWeapon("WEAPON_PISTOL50", 10000, true);
                officer.RelationshipGroup = "COP";
                PoliceOfficersAimingSpawned.Add(officer);
                PoliceOfficersSpawned.Add(officer);
                AllBankHeistEntities.Add(officer);
                officer.CanAttackFriendlies = false;

                float d1 = Vector3.Distance(officer.Position, PacificBankDoors[0]);
                float d2 = Vector3.Distance(officer.Position, PacificBankDoors[1]);
                Vector3 AimPoint = d1 < d2 ? PacificBankDoors[0] : PacificBankDoors[1];
                
                Rage.Native.NativeFunction.Natives.TASK_AIM_GUN_AT_COORD(officer, AimPoint.X, AimPoint.Y, AimPoint.Z, -1, false, false);

            }

            CaptainWells = new Ped(new Model("ig_fbisuit_01"), CaptainWellsLocation, CaptainWellsHeading);
            Functions.SetPedCantBeArrestedByPlayer(CaptainWells, true);
            CaptainWells.BlockPermanentEvents = true;
            CaptainWells.IsPersistent = true;
            CaptainWells.IsInvincible = true;
            CaptainWells.RelationshipGroup = "COP";
            CaptainWellsBlip = CaptainWells.AttachBlip();
            CaptainWellsBlip.Color = Color.Green;

            AllBankHeistEntities.Add(CaptainWells);
        }
        //STATUS: refactored, review
        private void SpawnAllPoliceCars()
        {
            foreach (var v in PoliceVehicles)
            {
                v.Instance = new Vehicle(v.Model, v.Position, v.Heading);
                var i = v.Instance;
                i.IsPersistent = true;
                i.IsSirenOn = true;
                i.IsSirenSilent = true;
                AllBankHeistEntities.Add(i);
                if (v.Model == "RIOT") RiotVans.Add(i);
            }
        }
        //STATUS: refactored, review, apply changes to data structs?
        private void SpawnBothSwatTeams()
        {
            foreach (var op in SWATOperators)
            {
                op.Instance = SpawnSWATOperator(op);
                AllBankHeistEntities.Add(op.Instance);
            }

            //TODO:
            // - make 1 SWAT operators list
            // - use SwatOperators instead of SwatTeam1/2 AND swatUnitsSpawned
        }
        //STATUS: refactored, review
        private Ped SpawnSWATOperator(EntityData<Ped> data)
        {
            Ped unit = new Ped("s_m_y_swat_01", data.Position, data.Heading);
            Functions.SetPedAsCop(unit);
            Functions.SetCopAsBusy(unit, true);

            unit.CanBeTargetted = false;
            unit.BlockPermanentEvents = true;
            unit.IsPersistent = true;
            unit.Inventory.GiveNewWeapon(new WeaponAsset(SWATWeapons[AssortedCalloutsHandler.rnd.Next(SWATWeapons.Length)]), 10000, true);
            unit.RelationshipGroup = "COP";

            unit.Tasks.PlayAnimation("cover@weapon@rpg", "blindfire_low_l_enter_low_edge", 1f, AnimationFlags.StayInEndFrame);
            NativeFunction.Natives.SET_PED_PROP_INDEX(unit, 0, 0, 0, 2);
            NativeFunction.Natives.SetPedCombatAbility(unit, 2);
            unit.CanAttackFriendlies = false;
            unit.Health = 209;
            unit.Armor = 92;

            return unit;
        }
        //STATUS: refactored, review
        private void SpawnNegotiationRobbers()
        {
            foreach (var r in RobbersNegotiation)
            {
                var wpn = new WeaponAsset(RobbersWeapons[AssortedCalloutsHandler.rnd.Next(RobbersWeapons.Length)]);
                var s = SpawnRobber(r, wpn, "ROBBER", 145, 190);
                r.Instance = s;
                Robbers.Add(s);
                AllBankHeistEntities.Add(s);
            }
        }
        //STATUS: done
        private int GetRndRobberVariation() => AssortedCalloutsHandler.rnd.Next(2) == 0 ? 2 : 1;
        //STATUS: refactored, review
        private void SpawnSneakyRobbers()
        {
            foreach (var r in SneakyRobbers)
            {
                if (AssortedCalloutsHandler.rnd.Next(5) < 3) continue;

                var wpn = new WeaponAsset(RobbersSneakyWeapons[AssortedCalloutsHandler.rnd.Next(RobbersSneakyWeapons.Length)]);

                var s = SpawnRobber(r, wpn, "SNEAKYROBBERS", 80, 185);
                r.Instance = s;
                s.Tasks.PlayAnimation("cover@weapon@rpg", "blindfire_low_l_enter_low_edge", 1f, AnimationFlags.StayInEndFrame);

                AllBankHeistEntities.Add(s);
            }
        }
        //STATUS: refactored, review
        private Ped SpawnRobber(EntityData<Ped> data, WeaponAsset wpn, string relGroup, int armor, int addHealth)
        {
            Ped unit = new Ped("mp_g_m_pros_01", data.Position, data.Heading);

            NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(unit, 9, GetRndRobberVariation(), 1, 0);
            Functions.SetPedCantBeArrestedByPlayer(unit, true);

            unit.IsPersistent = true;
            unit.BlockPermanentEvents = true;
            unit.Inventory.GiveNewWeapon(wpn, 10000, true);
            unit.RelationshipGroup = relGroup;
            Rage.Native.NativeFunction.Natives.SetPedCombatAbility(unit, 3);
            unit.CanAttackFriendlies = false;
            unit.Armor = armor;
            unit.Health += addHealth;

            return unit;
        }
        //STATUS: to be refactored
        private void SpawnAssaultRobbers()
        {
            for (int i = 0; i < RobbersAssaultLocations.Count; i++)
            {
                Ped unit = new Ped("mp_g_m_pros_01", RobbersAssaultLocations[i], RobbersAssaultHeadings[i]);

                var val = GetRndRobberVariation();
                NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(unit, 9, val, 1, 0);

                Functions.SetPedCantBeArrestedByPlayer(unit, true);
                unit.IsPersistent = true;
                unit.BlockPermanentEvents = true;
                unit.Inventory.GiveNewWeapon(new WeaponAsset(RobbersWeapons[AssortedCalloutsHandler.rnd.Next(RobbersWeapons.Length)]), 10000, true);
                unit.Inventory.GiveNewWeapon(new WeaponAsset(Grenades[AssortedCalloutsHandler.rnd.Next(Grenades.Length)]), 4, false);
                unit.RelationshipGroup = "ROBBERS";
                Rage.Native.NativeFunction.Natives.SetPedCombatAbility(unit, 3);
                unit.CanAttackFriendlies = false;
                unit.Armor = 238;
                unit.Health += 280;

                Robbers.Add(unit);
                AllBankHeistEntities.Add(unit);
            }
        }
        ////STATUS: refactored, review
        private void SpawnVaultRobbers()
        {
            foreach (var r in VaultRobbers)
            {
                var wpn = new WeaponAsset("WEAPON_ASSAULTSMG");
                var s = SpawnRobber(r, wpn, "ROBBERS", 95, 230);
                r.Instance = s;
                AllBankHeistEntities.Add(s);
            }
        }
        //STATUS: to be refactored, extract common core from Spawn f()'s
        private void SpawnMiniGunRobber()
        {
            MiniGunRobber = new Ped("mp_g_m_pros_01", MiniGunRobberLocation, MiniGunRobberHeading);

            var val = GetRndRobberVariation();
            NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(MiniGunRobber, 9, val, 1, 0);

            Functions.SetPedCantBeArrestedByPlayer(MiniGunRobber, true);
            MiniGunRobber.IsPersistent = true;
            MiniGunRobber.BlockPermanentEvents = true;
            MiniGunRobber.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_COMBATMG"), 1000, true);
            //MiniGunRobber.IsPositionFrozen = true;
            Rage.Native.NativeFunction.Natives.SetPedCombatAbility(MiniGunRobber, 3);
            MiniGunRobber.RelationshipGroup = "ROBBERS";
            MiniGunRobber.CanAttackFriendlies = false;
            Rage.Native.NativeFunction.Natives.SET_PED_DROPS_WEAPONS_WHEN_DEAD(MiniGunRobber, false);
            Rage.Native.NativeFunction.Natives.SET_PED_SHOOT_RATE(MiniGunRobber, 1000);
            MiniGunRobber.Armor = 60;
            MiniGunRobber.Health += 185;
            AllBankHeistEntities.Add(MiniGunRobber);
        }
        //STATUS: to be refactored
        private void HandleCustomRespawn()
        {
            HandlingRespawn = true;
            SWATFollowing = false;
            MiniGunRobberFiring = false;
            TimesDied++;
            AudioState OldAudioState = CurrentAudioState;
            CurrentAudioState = AudioState.None;
            AudioStateChanged = true;
            GameFiber.StartNew(delegate
            {
                while (true)
                {
                    GameFiber.Yield();
                    if (Game.IsScreenFadedOut)
                    {
                        break;
                    }
                }
                GameFiber.Sleep(1000);
                while (true)
                {
                    GameFiber.Yield();
                    if (Game.LocalPlayer.Character.Exists())
                    {
                        if (Game.LocalPlayer.Character.IsAlive)
                        {
                            break;
                        }
                    }
                }
                Game.LocalPlayer.HasControl = false;
                Game.FadeScreenOut(1, true);
                Game.LocalPlayer.Character.WarpIntoVehicle(Ambulances[0].Instance, 2);


                Game.FadeScreenIn(2500, true);
                Game.LocalPlayer.HasControl = true;
                CurrentAudioState = OldAudioState;
                AudioStateChanged = true;
                Player.WarpIntoVehicle(Ambulances[0].Instance, 2);

                GameFiber.Yield();

                if (Player.IsInVehicle(Ambulances[0].Instance, false))
                {
                    Player.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();
                }

                while (true)
                {
                    GameFiber.Yield();
                    if (Vector3.Distance(PlayerPosition, Ambulances[0].Instance.Position) < 70f)
                    {
                        break;
                    }
                    if (Player.IsAlive)
                    {
                        Game.DisplayHelp("Press ~b~Enter ~s~when spawned to spawn to the ambulance.");
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(Keys.Enter))
                        {
                            Player.WarpIntoVehicle(Ambulances[0].Instance, 2);
                            Game.HideHelp();
                            GameFiber.Sleep(1000);
                        }
                    }
                }

                MiniGunRobberFiring = false;
                HandlingRespawn = false;
            });
        }

        public override void End()
        {
            base.End();
            CalloutRunning = false;
            AlarmPlayer.Stop();

            SpeechHandler.DisplayTime = false;
            SpeechHandler.DisplayingBankHeistSpeech = false;

            Game.LocalPlayer.Character.IsPositionFrozen = false;
            Game.LocalPlayer.HasControl = true;
            //Game.LocalPlayer.Character.CanAttackFriendlies = false;

            Rage.Native.NativeFunction.Natives.SET_PLAYER_WEAPON_DEFENSE_MODIFIER(Game.LocalPlayer, 1f);
            Rage.Native.NativeFunction.Natives.SET_PLAYER_WEAPON_DAMAGE_MODIFIER(Game.LocalPlayer, 1f);
            Rage.Native.NativeFunction.Natives.RESET_AI_WEAPON_DAMAGE_MODIFIER();
            Rage.Native.NativeFunction.Natives.RESET_AI_MELEE_WEAPON_DAMAGE_MODIFIER();

            if (SideDoorBlip.Exists()) { SideDoorBlip.Delete(); }
            if (MobilePhone.Exists()) { MobilePhone.Delete(); }
            ToggleMobilePhone(Game.LocalPlayer.Character, false);

            if (!CalloutFinished)
            {
                if (Maria.Exists()) { Maria.Delete(); }
                if (MariaCop.Exists()) { MariaCop.Delete(); }
                if (MariaCopCar.Exists()) { MariaCopCar.Delete(); }
                if (CaptainWells.Exists())
                {
                    CaptainWells.Delete();
                }
                if (CaptainWellsBlip.Exists())
                {
                    CaptainWellsBlip.Delete();
                }
                if (MiniGunRobber.Exists())
                {
                    MiniGunRobber.Delete();
                }

                foreach (Ped i in Robbers)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in RobbersVault)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in FiremenList)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in ParamedicsList)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in RobbersSneakySpawned)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in AllHostages)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                foreach (Ped i in PoliceOfficersSpawned)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }

                SWATOperators.ForEach(o => DeleteEntity(o.Instance));

                foreach (Vehicle i in FireTrucksList)
                {
                    if (i.Exists()) { i.Delete(); }
                }


                foreach (var b in Barriers_)
                {
                    DeleteEntity(b.Instance);
                    DeleteEntity(b.InvisibleWall);
                    DeleteEntity(b.Ped);
                }
            }
            else
            {
                foreach (Ped i in RobbersVault)
                {
                    if (i.Exists())
                    {
                        i.Delete();
                    }
                }
                if (Maria.Exists()) { Maria.Dismiss(); }
                if (MariaCop.Exists()) { MariaCop.Dismiss(); }
                if (MariaCopCar.Exists()) { MariaCopCar.Dismiss(); }
                if (CaptainWells.Exists())
                {
                    CaptainWells.Dismiss();
                }
                if (CaptainWellsBlip.Exists())
                {
                    CaptainWellsBlip.Delete();
                }
                if (MiniGunRobber.Exists())
                {
                    if (MiniGunRobber.IsAlive) { MiniGunRobber.Delete(); }
                    else
                    {
                        MiniGunRobber.Dismiss();
                    }
                }
                foreach (Ped i in FiremenList)
                {
                    if (i.Exists())
                    {
                        i.Dismiss();
                    }
                }
                foreach (Ped i in ParamedicsList)
                {
                    if (i.Exists())
                    {
                        i.Dismiss();
                    }
                }
                foreach (Vehicle i in FireTrucksList)
                {
                    if (i.Exists())
                    {
                        Ped driver;
                        if (i.HasDriver) { driver = i.Driver; }
                        else { driver = i.CreateRandomDriver(); }

                        if (driver.Exists())
                        {
                            driver.Tasks.CruiseWithVehicle(i, 14f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
                            driver.Dismiss();
                        }
                        i.Dismiss();
                    }
                }



                Ambulances.ForEach(a => DeleteEntity(a.Instance));
                foreach (Vehicle i in AmbulancesList)
                {
                    if (i.Exists())
                    {
                        Ped driver;
                        if (i.HasDriver) { driver = i.Driver; }
                        else { driver = i.CreateRandomDriver(); }

                        if (driver.Exists())
                        {
                            driver.Tasks.CruiseWithVehicle(i, 14f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
                            driver.Dismiss();
                        }
                        i.Dismiss();
                    }
                }
                foreach (Ped i in RobbersSneakySpawned)
                {
                    if (i.Exists())
                    {
                        if (i.IsAlive) { i.Delete(); }
                        else
                        {
                            i.Dismiss();
                        }
                    }
                }
                foreach (Ped i in Robbers)
                {
                    if (i.Exists())
                    {
                        if (i.IsAlive) { i.Delete(); }
                        else
                        {
                            i.Dismiss();
                        }
                    }
                }
                foreach (Ped i in AllHostages)
                {
                    if (i.Exists())
                    {
                        i.Dismiss();
                    }
                }
                foreach (Ped i in PoliceOfficersSpawned)
                {
                    if (i.Exists())
                    {
                        i.Dismiss();
                    }
                }
                foreach (var p in PoliceVehicles)
                {
                    var i = p.Instance;
                    if (i.Exists())
                    {
                        Ped driver;
                        if (i.HasDriver) { driver = i.Driver; }
                        else { driver = i.CreateRandomDriver(); }

                        if (driver.Exists())
                        {
                            driver.Tasks.CruiseWithVehicle(i, 14f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.DriveAroundPeds);
                            driver.Dismiss();
                        }
                        i.Dismiss();
                    }
                }

                foreach (var b in Barriers_)
                {
                    DeleteEntity(b.Instance);
                    DeleteEntity(b.InvisibleWall);
                    DeleteEntity(b.Ped);
                }
            }
        }
    }

    internal partial class BankHeist : AssortedCallout
    {
        private class EntityData<T> where T : Rage.Entity
        {
            public string Model;
            public Vector3 Position;
            public float Heading;
            public T Instance;
        }
        private Vector3 DoorSide1 = new Vector3(265.542f, 217.4402f, 110.283f);
        private Vector3 DoorSide2 = new Vector3(265.8473f, 218.1096f, 110.283f);

        List<string> FinalDialogueText = new List<string>() { "Cpt. Wells: Thank god you've brought this all to an end!", "You: Yeah, it was pretty hectic in there!", "Cpt. Wells: I will send you the operation report soon.", "Cpt. Wells: Good work today officer, thank you.", "You: Just doing my job, sir. " };
        private List<string> AlarmAnswers = new List<string>() { "You: Yeah definitely, I can't hear myself think with that thing going.", "You: Nah, it doesn't bother me that much." };
        private static string[] LSPDModels = new string[] { "s_m_y_cop_01", "S_F_Y_COP_01" };
        private string[] SWATWeapons = new string[] { "WEAPON_CARBINERIFLE", "WEAPON_ASSAULTSMG" };
        private string[] Grenades = new string[] { "WEAPON_GRENADE", "WEAPON_SMOKEGRENADE" };
        private string[] HostageModels = new string[] { "A_F_M_BUSINESS_02", "A_M_M_BUSINESS_01", "A_F_Y_FEMALEAGENT", "A_M_Y_BUSINESS_03" };
        private string[] RobbersSneakyWeapons = new string[] { "WEAPON_PISTOL50", "WEAPON_KNIFE", "WEAPON_ASSAULTSMG", "WEAPON_ASSAULTSHOTGUN", "WEAPON_CROWBAR", "WEAPON_HAMMER", "WEAPON_ASSAULTRIFLE" };
        private string[] RobbersWeapons = new string[] { "WEAPON_SAWNOFFSHOTGUN", "WEAPON_ASSAULTRIFLE", "WEAPON_PUMPSHOTGUN", "WEAPON_ASSAULTSHOTGUN", "WEAPON_ADVANCEDRIFLE" };

        class BarrierData
        {
            public Vector3 Position;
            public float Heading;
            public Rage.Object Instance;
            public Ped Ped;
            public Rage.Object InvisibleWall;
        }

        List<BarrierData> Barriers_ = new List<BarrierData>
        {
            new BarrierData()
            {
                Position = new Vector3(215.393f, 203.157f, 104.454f),
                Heading = 286.3633f,
            },
            new BarrierData()
            {
                Position = new Vector3(215.1232f, 205.6814f, 104.4652f),
                Heading = 290.2363f,
            },
            new BarrierData()
            {
                Position = new Vector3(218.4388f, 196.256f, 104.5912f),
                Heading = 344.4589f,
            },
            new BarrierData()
            {
                Position = new Vector3(233.0477f, 191.5893f, 104.3578f),
                Heading = 346.3031f,
            },
            new BarrierData()
            {
                Position = new Vector3(235.1332f, 191.1562f, 104.3189f),
                Heading = 341.4462f,
            },
            new BarrierData()
            {
                Position = new Vector3(237.6775f, 190.3424f, 104.2726f),
                Heading = 342.168f,
            },
            new BarrierData()
            {
                Position = new Vector3(247.1391f, 188.03f, 104.0998f),
                Heading = 25.01121f,
            },
            new BarrierData()
            {
                Position = new Vector3(244.9249f, 187.9552f, 104.1492f),
                Heading = 1.558372f,
            },
            new BarrierData()
            {
                Position = new Vector3(218.238f, 213.5867f, 104.4652f),
                Heading = 255.0954f,
            },
            new BarrierData()
            {
                Position = new Vector3(218.7885f, 216.0675f, 104.4652f),
                Heading = 255.0954f,
            },
            new BarrierData()
            {
                Position = new Vector3(219.6092f, 218.8511f, 104.4652f),
                Heading = 267.3944f,
            },
        };

        private List<EntityData<Vehicle>> PoliceVehicles = new List<EntityData<Vehicle>>()
        {
            new EntityData<Vehicle>()
            {
                Model = "POLICE",
                Position = new Vector3(222.4914f, 196.139f, 105.2151f),
                Heading = 251.93f,
            },
            new EntityData<Vehicle>()
            {
                Model = "POLICE",
                Position = new Vector3(228.7804f, 193.9648f, 105.0773f),
                Heading = 70.06104f,
            },
            new EntityData<Vehicle>()
            {
                Model = "POLICE",
                Position = new Vector3(250.7617f, 190.7597f, 104.5666f),
                Heading = 291.0875f,
            },

            new EntityData<Vehicle>()
            {
                Model = "POLICE2",
                Position = new Vector3(216.4797f, 199.8008f, 105.1088f),
                Heading = 11.46685f,
            },
            new EntityData<Vehicle>()
            {
                Model = "POLICE2",
                Position = new Vector3(216.3862f, 209.5035f, 105.1084f),
                Heading = 339.02f,
            },
            new EntityData<Vehicle>()
            {
                Model = "POLICE3",
                Position = new Vector3(241.5773f, 190.1744f, 104.9979f),
                Heading = 246.1062f,
            },
            new EntityData<Vehicle>()
            {
                Model = "POLICE3",
                Position = new Vector3(223.8036f, 221.5969f, 105.2692f),
                Heading = 305.6254f,
            },

            new EntityData<Vehicle>()
            {
                Model = "RIOT",
                Position = new Vector3(224.1989f, 207.5056f, 105.1199f),
                Heading = 193.6453f,
            },
            new EntityData<Vehicle>()
            {
                Model = "RIOT",
                Position = new Vector3(263.7726f, 193.397f, 104.4452f),
                Heading = 214.4147f,
            },
        };

        private List<Vehicle> RiotVans = new List<Vehicle>(); //add when model == riot

        private List<EntityData<Vehicle>> Ambulances = new List<EntityData<Vehicle>>()
        {
            new EntityData<Vehicle>()
            {
                Position = new Vector3(260.8994f, 166.494f, 104.5317f),
                Heading = 199.5887f,
            },
            new EntityData<Vehicle>()
            {
                Position = new Vector3(239.1898f, 172.3954f, 104.8571f),
                Heading = 158.5571f,
            }
        };

        private List<EntityData<Ped>> Paramedics = new List<EntityData<Ped>>()
        {
            new EntityData<Ped>()
            {
                Position = new Vector3(242.0103f, 174.1728f, 105.1191f),
                Heading = 330.9329f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(243.9722f, 170.8235f, 105.0307f),
                Heading = 341.8453f,
            },
        };

        private List<EntityData<Vehicle>> Firetrucks = new List<EntityData<Vehicle>>()
        {
            new EntityData<Vehicle>()
            {
                Position = new Vector3(246.3588f, 167.8176f, 104.9527f),
                Heading = 249.4772f,
            },
        };

        private List<EntityData<Ped>> Hostages = new List<EntityData<Ped>>()
        {
            new EntityData<Ped>()
            {
                Position = new Vector3(253.4743f, 217.7294f, 106.2868f),
                Heading = 26.3351f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(240.2256f, 223.8581f, 106.2869f),
                Heading = 333.7643f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(247.4731f, 215.7981f, 106.2869f),
                Heading = 308.5362f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(235.0374f, 218.5802f, 110.2827f),
                Heading =  183.8941f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(243.3186f, 210.8154f, 110.283f),
                Heading =  69.50799f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(265.409f, 214.4903f, 110.2873f),
                Heading =  250.7003f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(256.4999f, 225.638f, 106.2868f),
                Heading =  344.9573f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(257.9439f, 227.4617f, 101.6833f),
                Heading = 66.61107f,
            },
        };

        private List<EntityData<Ped>> RescuedHostages = new List<EntityData<Ped>>();
        private List<EntityData<Ped>> SafeHostages = new List<EntityData<Ped>>();

        private List<EntityData<Ped>> RobbersNegotiation = new List<EntityData<Ped>>()
        {
            new EntityData<Ped>()
            {
                Position = new Vector3(235.2906f, 217.1142f, 106.2867f),
                Heading = 109.0629f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(254.4529f, 217.6757f, 106.2868f),
                Heading = 230.5565f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(243.2524f, 222.3944f, 106.2868f),
                Heading = 78.30953f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(257.5506f, 223.6651f, 106.2863f),
                Heading = 139.71f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(242.9586f, 213.7329f, 110.283f),
                Heading = 333.9886f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(261.4425f, 223.766f, 101.6833f),
                Heading = 246.4011f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(266.9025f, 219.2729f, 104.8833f),
                Heading = 93.847f,
            }
        };

        private readonly List<EntityData<Ped>> SneakyRobbers = new List<EntityData<Ped>>()
        {
            new EntityData<Ped>()
            {
                Position = new Vector3(235.5733f, 228.3068f, 110.2827f),
                Heading = 248.8268f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(256.7757f, 205.0848f, 110.283f),
                Heading = 339.1755f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(265.3547f, 222.4385f, 101.6833f),
                Heading = 153.5341f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(263.0323f, 215.4664f, 110.2877f),
                Heading = 159.1769f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(255.1933f, 222.045f, 106.2869f),
                Heading = 341.1396f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(238.6139f, 228.2485f, 106.2834f),
                Heading = 69.37666f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(238.6164f, 227.2258f, 110.2827f),
                Heading = 68.82679f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(261.3226f, 210.6962f, 110.2877f),
                Heading = 166.2358f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(265.8036f, 215.6155f, 110.283f),
                Heading = 334.0478f,
            },
        };

        private List<EntityData<Ped>> VaultRobbers = new List<EntityData<Ped>>()
        {
            new EntityData<Ped>()
            {
                Position = new Vector3(253.9261f, 221.6735f, 101.6834f),
                Heading = 353.9462f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(252.686f, 221.9205f, 101.6834f),
                Heading = 343.3658f,
            },
            new EntityData<Ped>()
            {
                Position = new Vector3(251.4069f, 222.5131f, 101.6834f),
                Heading = 335.4267f,
            },
        };

        private Vector3[] PacificBankDoors = new Vector3[] { new Vector3(229.7984f, 214.4494f, 105.5554f), new Vector3(258.3625f, 200.4897f, 104.9758f) };
        private Vector3[] PacificBankInsideChecks = new Vector3[] { new Vector3(235.9762f, 220.6012f, 106.2868f), new Vector3(238.3628f, 214.8286f, 106.2868f), new Vector3(261.084f, 208.12f, 106.2832f), new Vector3(235.2972f, 217.1385f, 106.2867f) };
        private Vector3[] PacificBankDoorsInside = new Vector3[] { new Vector3(259.5908f, 204.1841f, 106.2832f), new Vector3(232.4167f, 215.6826f, 106.2866f) };

        private Vector3 InsideBankVault = new Vector3(252.3106f, 222.5586f, 101.6834f);
        private Vector3 OutsideBankVault = new Vector3(257.3354f, 225.5874f, 101.8757f);
    }
}
