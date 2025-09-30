using Challenges.ChallengeScripts;

namespace Challenges
{
    public static class BuiltinChallenges
    {
        public static void RegisterAll()
        {
            ChallengesAPI.RegisterChallenge(
                new Challenge(
                    "permasnow",
                    "Frostbite",
                    " - Ascent 5\n" +
                    " - It is always storming in The Alpines\n" +
                    " - It is always raining in The Tropics\n" +
                    " - The Alpines is forced\n" +
                    "PLEASE USE THE FAIR SNOWSTORMS MOD.",
                    ChallengeDifficulty.MEDIUMCORE,
                    ascentOverride: 5
                    )
                );
            ChallengesAPI.RegisterChallenge(
                new Challenge(
                    "instagib",
                    "Instagib",
                    " - Ascent 7\n" +
                    " - Going unconcious instantly kills you.\n",
                    ChallengeDifficulty.HARD,
                    ascentOverride: 7
                )
            );
            ChallengesAPI.RegisterChallenge(
                new Challenge(
                    "cappedluggage",
                    "Baggage Allowance",
                    " - Ascent 1\n" +
                    " - Only one item in luggage\n",
                    ChallengeDifficulty.EASY,
                    ascentOverride: 1
                )
            );
            ChallengesAPI.RegisterChallenge(
                new Challenge(
                    "hunger",
                    "The Hunger",
                    " - Ascent 3\n" +
                    " - Stamina only recovers after eating\n",
                    ChallengeDifficulty.HARDCORE,
                    ascentOverride: 3
                )
            );
            ChallengesAPI.RegisterChallenge(
                new Challenge(
                    "inversion",
                    "Inversion",
                    " - Ascent 4\n" +
                    " - Natural affliction recovery is reversed\n",
                    ChallengeDifficulty.HARD,
                    ascentOverride: 4
                )
            );
            ChallengesAPI.RegisterChallenge(
                new Challenge(
                    "narcolepsy",
                    "Narcolepsy",
                    " - Ascent 4\n" +
                    " - Knocked out by drowsiness every 1-3 minutes\n",
                    ChallengeDifficulty.MEDIUM,
                    ascentOverride: 4
                )
            );
            ChallengesAPI.RegisterChallenge(
                new Challenge(
                    "afflictionrandomizer",
                    "Affliction Randomizer",
                    " - Ascent 5\n" +
                    " - Added statuses become random afflictions\n",
                    ChallengeDifficulty.HARD,
                    ascentOverride: 5
                )
            );
            ChallengesAPI.RegisterChallenge(
                new Challenge(
                    "scoutmasterlurks",
                    "Scoutmaster Lurks",
                    " - Ascent Tenderfoot (-1)\n" +
                    " - Scoutmaster only hunts at night\n" +
                    " - He always goes for the closest camper\n",
                    ChallengeDifficulty.MEDIUM,
                    ascentOverride: -1
                )
            );
            ChallengesAPI.RegisterChallenge(
                new Challenge(
                    "planecrash",
                    "Plane Crash",
                    " - Ascent Tenderfoot (-1)\n" +
                    " - Start with 90% injury\n",
                    ChallengeDifficulty.LIGHT,
                    ascentOverride: -1
                )
            );
            // hell always goes last
            ChallengesAPI.RegisterChallenge(
                new Challenge(
                    "hell",
                    "Hell",
                    " - Ascent 1337\n" +
                    " - All other challenges are enabled\n",
                    ChallengeDifficulty.HARDCORE,
                    ascentOverride: 1337
                )
            );

            // load helpers
            ChooseBiome.Init();
            SnowPatch.Init();
        }
    }
}
