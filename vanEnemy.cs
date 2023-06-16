using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class vanEnemy : MonoBehaviour
{
    private float radius = 10f; //range of enemies vision
    private float killRange = 0.7f; //enemy kill range
    private float lookingTime = 16f;
    private DateTime lookingStart; //start time of looking sequence

    private PlayerMovement1stPerson player;

    //sequence booleans
    bool retreating;
    bool looking;
    bool chasing;

    void Update(){
        if(looking){
            //start enemy looking for player sequence
            StartCoroutine(LookingForPlayer());
        }
        else if(retreating){
            //start enemy retreating to van sequence
            StartCoroutine(Retreating());
        }
        else if(chasing){
            //start enemy chasing player sequence
            StartCoroutine(ChasingPlayer());
        }
    }
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position ,radius);
    }
    //setup chasing sequence
    public void Chase(){
        gameObject.GetComponent<Animator>().SetBool("isRunning", true);
        player = GameObject.FindObjectOfType<PlayerMovement1stPerson>();
        chasing = true;
    }
    //check if player in range of enemy
    private bool PlayerInRange(){
        return Vector3.Distance(player.transform.position, transform.position)<=radius&&!(player.GetComponent<PlayerMovement1stPerson>().isHome);
    }
    IEnumerator Retreating(){
        chasing = false;
        looking = false;
        if(PlayerInRange()){
            StopCoroutine(Retreating());
            chasing = true;
        }
        else{
            yield return new WaitUntil(()=>Vector3.Distance(transform.position, GameObject.FindObjectOfType<Van>().transform.position)<=2f);
            GameObject.FindObjectOfType<Van>().Continue();
            Destroy(gameObject);
        }
    }
    IEnumerator LookingForPlayer(){
        retreating = false;
        chasing = false;
        // if player re-enters vision range
        if(PlayerInRange()){
            StopCoroutine(LookingForPlayer());
            chasing = true;
        }
        yield return new WaitUntil(()=> DateTime.Now-lookingStart>=TimeSpan.FromSeconds(lookingTime));
        gameObject.GetComponent<Animator>().SetBool("isLooking", false);
        gameObject.GetComponent<Animator>().SetBool("isRunning", true);
        this.GetComponent<NavMeshAgent>().SetDestination(GameObject.FindObjectOfType<Van>().transform.position);
        retreating = true;
    }
    IEnumerator ChasingPlayer(){
        retreating = false;
        looking = false;
        //if player exits vision range
        if(!PlayerInRange()){
            gameObject.GetComponent<Animator>().SetBool("isLooking", true);
            gameObject.GetComponent<Animator>().SetBool("isRunning", false);
            this.transform.rotation = Quaternion.LookRotation(player.transform.position);
            lookingStart = DateTime.Now;
            StopCoroutine(ChasingPlayer());
            looking = true;
        }
        else{
            this.GetComponent<NavMeshAgent>().SetDestination(player.transform.position);
            //if player in enemy kill range
            yield return new WaitUntil(()=> Vector3.Distance(transform.position, player.transform.position)<=killRange);
            KillPlayer();
        }
    }
    //if killer gets close enough to the player
    private void KillPlayer(){
        GameObject.FindObjectOfType<GameManagerScript>().GameOver(gameObject.name);
    }
}
