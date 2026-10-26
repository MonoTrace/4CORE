using System.Globalization;
using UnityEngine;

namespace FourCore
{
    public static class HighScoreStore
    {
        private const string Key="4CORE.HighScore.v1";
        public static long Best => long.TryParse(PlayerPrefs.GetString(Key,"0"),NumberStyles.Integer,CultureInfo.InvariantCulture,out long value)?System.Math.Max(0,value):0;
        public static bool Submit(long score)
        {
            if(score<=Best)return false;
            PlayerPrefs.SetString(Key,score.ToString(CultureInfo.InvariantCulture));PlayerPrefs.Save();return true;
        }
    }
}
