using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scenes.ObjectsData;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scrpits.Location
{
    public class DoorDisplay : MonoBehaviour
    {
        public SpriteRenderer iconLoc;
        public string sceneToLoad;

        public void SetLocation(LocationData location)
        {
            iconLoc.sprite = location.locationSprite;
            sceneToLoad = location.sceneName;
        }

        private void OnMouseDown()
        {
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneManager.LoadScene(sceneToLoad);
            }
        }
    }
}
