using UnityEngine;

namespace SER.LastManStanding
{
    [CreateAssetMenu(fileName = "NewGameCharacter", menuName = "Game Characters/GameCharacter")]
    public class GameCharacters : ScriptableObject
    {
        public string characterName;
        public GameObject characterPrefab;
        public string[] abilities;
    }
}
