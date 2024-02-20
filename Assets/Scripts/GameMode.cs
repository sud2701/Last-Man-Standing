using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SER.LastManStanding
{
    public enum GameMode
    {
        DM = 0,
        TDM = 1,
        BR = 2
    }

    public class GameSettings : MonoBehaviour
    {
        public static GameMode GameMode = GameMode.DM;
        public static bool IsAwayTeam = false;
    }
}