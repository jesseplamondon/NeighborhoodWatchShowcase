using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class LaptopTakePhoto : MonoBehaviour
{
    public Camera lapCam;
    public bool active = false;
    public GameObject takePhotoUI;
    public GameObject noActiveCamsUI;
    public PlayerMovement1stPerson player;
    public MeshRenderer screen;
    public RenderTexture rt;
    public RenderTexture placeHold;

    void Update(){
        if(active){
            if(player.cameras.Count>0){
                takePhotoUI.SetActive(true);
                noActiveCamsUI.SetActive(false);
            }
            else{
                noActiveCamsUI.SetActive(true);
            }
        }
        else{
            takePhotoUI.SetActive(false);
            noActiveCamsUI.SetActive(false);
        }
    }
    public void ToggleRight(){
        if(player.cameras.Count>0){
            if(player.currentCamLaptop<player.cameras.Count-1){
                player.currentCamLaptop++;
            }
            else{
                player.currentCamLaptop=0;
            }
            for(int i = 0; i<player.cameras.Count;i++){
                if(i!=player.currentCamLaptop){
                    player.cameras[i].cameraCam.targetTexture = placeHold;
                }
            }
            player.cameras[player.currentCamLaptop].cameraCam.targetTexture = rt;
        }
            screen.material.mainTexture = rt;
    }
    public void ToggleLeft(){
        if(player.cameras.Count>0){
            if(player.currentCamLaptop>0){
                player.currentCamLaptop--;
            }
            else{
                player.currentCamLaptop=player.cameras.Count-1;
            }
            for(int i = 0; i<player.cameras.Count;i++){
                if(i!=player.currentCamLaptop){
                    player.cameras[i].cameraCam.targetTexture = placeHold;
                }
            }
            player.cameras[player.currentCamLaptop].cameraCam.targetTexture = rt;
        }
            screen.material.mainTexture = rt;
    }
    public void takeLaptopPhoto(){
        Camera_Interaction camItemDevice = player.cameras[player.currentCamLaptop];
        camItemDevice.takePhoto();
    }
}
