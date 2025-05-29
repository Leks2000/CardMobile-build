using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scenes.ObjectsData;
using Assets.Scrpits.Location;
using UnityEngine;

namespace Assets.Scrpits.GameManagers
{
    public class LocationControls : MonoBehaviour
    {
        public List<LocationData> allLocs;
        private LocationData leftDoor;
        private LocationData rightDoor;

        public DoorDisplay leftloc;
        public DoorDisplay rightloc;

        private EndTurnCamera turnCamera;

        private void Awake()
        {
            turnCamera = FindObjectOfType<EndTurnCamera>().GetComponent<EndTurnCamera>();
        }

        public void NextLevelCurrent()
        {
            var curentLocation = allLocs.OrderBy(x => UnityEngine.Random.value).Take(2).ToList();
            leftDoor = curentLocation[0];
            rightDoor = curentLocation[1];

            leftloc.SetLocation(leftDoor);
            rightloc.SetLocation(rightDoor);
        }


        public void MoveCameraAndSpawnDoors()
        {
            NextLevelCurrent();
        }
    }
}
