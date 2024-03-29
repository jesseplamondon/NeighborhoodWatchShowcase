using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;
using Random=UnityEngine.Random;
using random = System.Random;

public class Convict : MonoBehaviour
{
    [HideInInspector]public List<Transform> searchingDestinations;
    [HideInInspector]public ConvictStateMachine stateMachine;

    void Awake(){
        stateMachine = new ConvictStateMachine();
        stateMachine.Init();
        for(int i = 0; i<transform.childCount; i++){
            transform.GetChild(i).gameObject.SetActive(false);
        }
        Transform destinationsTransform = GameObject.Find("ConvictSearchingDestinations").transform;
        for(int i = 0; i<destinationsTransform.childCount; i++){
            searchingDestinations.Add(destinationsTransform.GetChild(i));
        }
    }
    void Update(){
        stateMachine.Update();
    }
}
public class ConvictStateMachine{
    public PlayerMovement1stPerson player;
    public Convict convict;
    public ConvictState currentState;

    public void Init(){
        player = GameObject.FindObjectOfType<PlayerMovement1stPerson>();
        convict = GameObject.FindObjectOfType<Convict>();
        currentState = new Waiting();
        currentState.SetPlayerConvict(player, convict);
        currentState.Enter();
    }
    public void Update(){
        currentState!.Execute();
    }
}
public class ConvictState{
    public enum States{SearchingForPlayer, ConvictLeaving, ConvictSitting, ConvictKilling}
    public string animationString = "isWalking";
    public PlayerMovement1stPerson player;
    public Convict convict;
    public virtual void Enter(){
        GameObject.FindObjectOfType<Convict>().GetComponent<Animator>().SetBool(animationString, true);
    }
    public virtual void Execute(){}
    public virtual void Exit(){GameObject.FindObjectOfType<Convict>().GetComponent<Animator>().SetBool(animationString, false);}
    public void SetPlayerConvict(PlayerMovement1stPerson player, Convict convict){
        this.player = player;
        this.convict = convict;
    }
    public void ChangeState(States s){
        ConvictStateMachine stateMachine = GameObject.FindObjectOfType<Convict>().stateMachine;
        if(stateMachine.currentState!=null){
            stateMachine.currentState.Exit();
        }
        stateMachine.currentState = (ConvictState)Activator.CreateInstance(Type.GetType(s.ToString()));
        stateMachine.currentState.SetPlayerConvict(player,convict);
        stateMachine.currentState.SetPlayerConvict(player, convict);
        stateMachine.currentState.Enter();
    }
}
public class Waiting:ConvictState{
    DateTime start;
    int waitTime = 14;
    public override void Enter(){
        start = DateTime.Now;
    }
    public override void Execute(){
        if(DateTime.Now-start>=TimeSpan.FromSeconds(waitTime)){
            ChangeState(States.SearchingForPlayer);
        }
    }
    public override void Exit(){
        for(int i = 0; i<convict.transform.childCount; i++){
            convict.transform.GetChild(i).gameObject.SetActive(true);
        }
    }
}
public class SearchingForPlayer:ConvictState{
    Vector3 destination;
    int destinationIndex;
    bool atDestination;
    DateTime start;
    int lookTime = 12;
    public override void Enter(){
        base.Enter();
        destination = convict.searchingDestinations[destinationIndex].position;
        destinationIndex++;
        convict.GetComponent<NavMeshAgent>().SetDestination(destination);
    }
    public override void Execute(){
        if(Vector3.Distance(destination, convict.transform.position)<=0.7f&&!atDestination){
            if(destinationIndex==convict.searchingDestinations.Count){
                ChangeState(States.ConvictSitting);
                return;
            }
            atDestination=true;
            start = DateTime.Now;
            //set animation type
            convict.StartCoroutine(Looking());
        }
    }
    IEnumerator Looking(){
        yield return new WaitUntil(()=>DateTime.Now-start>=TimeSpan.FromSeconds(lookTime));
        atDestination = false; 
        destination = convict.searchingDestinations[destinationIndex].position;
        destinationIndex++;
        convict.GetComponent<NavMeshAgent>().SetDestination(destination);
    }
}
public class ConvictSitting:ConvictState{
    DateTime start;
    int sitTime = 20;
    public override void Enter(){
        animationString = "Sitting";
        start = DateTime.Now;
        Quaternion.Slerp(convict.transform.rotation, Quaternion.identity, 1f*Time.deltaTime);
    }
    public override void Execute(){
        if(DateTime.Now-start>=TimeSpan.FromSeconds(sitTime)){
            ChangeState(States.ConvictLeaving);
        }
    }
}
public class ConvictLeaving:ConvictState{
    Vector3 destination;
    public override void Enter(){
        base.Enter();
        destination = GameObject.FindObjectOfType<BusStop>().convictStartTransform.position;
        convict.GetComponent<NavMeshAgent>().SetDestination(destination);
    }
    public override void Execute(){
        if(Vector3.Distance(destination, convict.transform.position)<=1f){
            GameObject.Destroy(convict.gameObject);
        }
    }
}
public class ConvictKilling:ConvictState{
    DateTime start;
    public override void Enter(){
        animationString = "isKilling";
        base.Enter();
    }
    void GameOver(){
        GameObject.FindObjectOfType<GameManagerScript>().GameOver("Convict");
    }
}
