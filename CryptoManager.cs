using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random=System.Random;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CryptoManager : MonoBehaviour
{
    public List<Stock> OwnedStocks;
    public List<int> QuantityOwned;
    public List<Stock> AvailableStocks;
    public Stock CurrentGraphStock;
    private int updateIntervalSeconds = 10;
    private float priceIncrementMin = 0.01f;
    private float priceIncrementMax = 0.051f;
    private DateTime updateStart;
    public GameObject BuySellStock;
    Random updateRnd;
    public GameObject OwnedStockFab;
    public GameObject SearchedStockFab;
    public GameObject SearchArea;
    bool stockShown = false;
    public GameObject GraphFrameFab;

    public GameObject SitePostPrefab;
    public TradingPost t;
    public DateTime postWaitStart;
    public DateTime postCreateStart;
    private float postDuration = 35f;
    private float waitToPost = 23f;
    public GameObject Col1;

    void Start(){
        t = new TradingPost();
        OwnedStocks = new List<Stock>();
        updateStart = DateTime.Now;
        updateRnd = new Random();
        postWaitStart = DateTime.Now;
    }
    void Update(){
        if(DateTime.Now-updateStart>=TimeSpan.FromSeconds(updateIntervalSeconds)){
            UpdateStockPrices();
        }
        if(DateTime.Now-postWaitStart>=TimeSpan.FromSeconds(waitToPost)&&t.title.Length==0){
            CreatePost();
        }
        else if(t.title.Length!=0&&DateTime.Now-postCreateStart>=TimeSpan.FromSeconds(postDuration)){
            EndPost();
        }
        if(Input.GetMouseButtonDown(0)){
            if(IsPointerOverElement("Buy")){
                if(!OwnedStocks.Contains(BuySellStock.GetComponent<SearchedStock>().stock)){
                    BuyStock(BuySellStock.GetComponent<SearchedStock>().stock);
                }
                else{
                    BuyMoreStock(BuySellStock.GetComponent<SearchedStock>().stock);   
                }
            }
            else if(IsPointerOverElement("BuyMore")){
                BuyMoreStock(BuySellStock.GetComponent<OwnedStockSlot>().stock);
            }
            else if(IsPointerOverElement("Sell")){
                SellStock(BuySellStock.GetComponent<OwnedStockSlot>().stock);
            }
            else if(IsPointerOverElement("GraphButton")){
                SetGraphValues(BuySellStock.GetComponent<SearchedStock>().stock);
            }
        }
        if(SearchArea.GetComponent<TMP_Text>().text.Length>0){
            if(Input.GetKeyDown(KeyCode.Return)){
                Search(SearchArea.transform.parent.parent.GetComponent<TMP_InputField>().text);
            }
        }
    }
    public void CreatePost(){
            GameObject newPost = Instantiate(SitePostPrefab);
            newPost.transform.parent = Col1.transform.GetChild(0);
            newPost.AddComponent<TradingPost>();
            newPost.GetComponent<TradingPost>().init(AvailableStocks[updateRnd.Next(0, AvailableStocks.Count)]);
            t = newPost.GetComponent<TradingPost>();
            if(t.bullish){
                SetBullish(t.stock);
            }
            else{
                SetBearish(t.stock);
            }
            postCreateStart = DateTime.Now;
    }
    public void EndPost(){
        AvailableStocks.Find(e=> e.ticker==t.stock.ticker).RemoveSentiment();
        postWaitStart = DateTime.Now;
        t= new TradingPost();
    }
    public void SetBearish(Stock s){
        s.bearish = true;
    }
    public void SetBullish(Stock s){
        s.bullish = true;
    }
    public void Exit(){
        GameObject.Find("CryptoCanvas").SetActive(false);
        GameObject.Find("LaptopScreen").GetComponent<Laptop_Home_Screen>().isCoin=false;
    }
    public void SetMoneyText(double amount){
        Text money = GameObject.Find("money").GetComponent<Text>();
        money.text = Math.Round(amount, 4).ToString();
    }
    public void BuyStock(Stock stock){
        PlayerMovement1stPerson player = GameObject.Find("Player").GetComponent<PlayerMovement1stPerson>();
        if(player.money>=stock.price){
            OwnedStocks.Add(stock);
            player.money-=stock.price;
            SetMoneyText(player.money);
            OwnedStockSlot slot = Instantiate(OwnedStockFab, Vector3.zero, Quaternion.identity).GetComponent<OwnedStockSlot>();
            slot.transform.SetParent(GameObject.Find("OwnedStocks").transform);
            slot.stock = stock;
            SetPriceText();
            QuantityOwned.Add(1);
        }
    }
    public void BuyMoreStock(Stock stock){
        int idx = OwnedStocks.IndexOf(stock);
        QuantityOwned[idx]++;
        PlayerMovement1stPerson player=GameObject.Find("Player").GetComponent<PlayerMovement1stPerson>();
        if(player.money>=stock.price){
            player.money-=stock.price;
            SetMoneyText(player.money);
            GameObject OwnedStockFabsParent = GameObject.Find("OwnedStocks");
            for(int i = 0; i<OwnedStockFabsParent.transform.childCount;i++){
                OwnedStockSlot s = OwnedStockFabsParent.transform.GetChild(i).GetComponent<OwnedStockSlot>();
                if(s.stock.sName==stock.sName){
                    s.NumOwned.GetComponent<TMP_Text>().text = QuantityOwned[idx].ToString();
                    break;
                }
            }
        }
    }
    public void SellStock(Stock stock){
        int idx = OwnedStocks.IndexOf(stock);
        QuantityOwned[idx]--;
        PlayerMovement1stPerson player=GameObject.Find("Player").GetComponent<PlayerMovement1stPerson>();
        player.money+=stock.price;
        SetMoneyText(player.money);
        if(QuantityOwned[idx]==0){
            QuantityOwned.RemoveAt(idx);
            OwnedStocks.Remove(stock);
            GameObject OwnedStockFabsParent = GameObject.Find("OwnedStocks");
            for(int i = 0; i<OwnedStockFabsParent.transform.childCount;i++){
                OwnedStockSlot s = OwnedStockFabsParent.transform.GetChild(i).GetComponent<OwnedStockSlot>();
                if(s.stock.sName==stock.sName){
                    Destroy(s.gameObject);
                    break;
                }
            }
        }
        else{
            GameObject OwnedStockFabsParent = GameObject.Find("OwnedStocks");
            for(int i = 0; i<OwnedStockFabsParent.transform.childCount;i++){
                OwnedStockSlot s = OwnedStockFabsParent.transform.GetChild(i).GetComponent<OwnedStockSlot>();
                if(s.stock.sName==stock.sName){
                    s.NumOwned.GetComponent<TMP_Text>().text = QuantityOwned[idx].ToString();
                    break;
                }
            }
        }
    }
    public void ShowAll(){
        GameObject left = GameObject.Find("CryptoBackground").transform.GetChild(1).transform.GetChild(0).gameObject;
        if(left.transform.childCount>0){
            for(int i = 0; i<left.transform.childCount;i++){
                Destroy(left.transform.GetChild(i).gameObject);
            }
        }
        foreach(Stock s in AvailableStocks){
            GameObject newSearch = Instantiate(SearchedStockFab, Vector3.zero, Quaternion.identity);
            newSearch.transform.SetParent(left.transform);
            newSearch.GetComponent<SearchedStock>().stock=s;
            newSearch.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text=s.sName;
        }
    }
    public void Search(string stockName){
        bool found = false;
        Stock SearchStock = new Stock();
        for(int i = 0; i<AvailableStocks.Count; i++){
            if(Equals(AvailableStocks[i].sName, stockName)||Equals(AvailableStocks[i].ticker, stockName)){
                SearchStock=AvailableStocks[i];
                found = true;
                break;
            }
        }
        if(!found){
            return;
        }
        GameObject searchedStocks = GameObject.Find("Left");
        //remove all existing search prefabs in left sidebar and instantiate one for searched stock
        if(searchedStocks.transform.childCount>0){
            for(int i = 0; i<searchedStocks.transform.childCount;i++){
                Destroy(searchedStocks.transform.GetChild(i).gameObject);
            }
        }
        GameObject newSearch = Instantiate(SearchedStockFab, Vector3.zero, Quaternion.identity);
        newSearch.transform.SetParent(searchedStocks.transform);
        newSearch.GetComponent<SearchedStock>().stock=SearchStock;
        newSearch.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text=stockName;
    }
    public void SetGraphValues(Stock stock){
        if(GameObject.Find("LaptopScreen").GetComponent<Laptop_Home_Screen>().isCoin){
            GameObject prevFrame = GameObject.Find("GraphFrame").gameObject;
            Destroy(prevFrame);
            GameObject GraphFrame = Instantiate(GraphFrameFab, prevFrame.transform.position, Quaternion.identity);
            GraphFrame.name = "GraphFrame";
            GraphFrame.transform.SetParent(prevFrame.transform.parent.transform);

            WindowGraph wG = GameObject.Find("CryptoCanvas").GetComponent<WindowGraph>();
            wG.graphContainer=GraphFrame.GetComponent<RectTransform>();
            wG.labelTemplateX = GraphFrame.transform.GetChild(0).GetComponent<RectTransform>();
            wG.labelTemplateY = GraphFrame.transform.GetChild(1).GetComponent<RectTransform>();
            wG.lineTemplateX = GraphFrame.transform.GetChild(2).GetComponent<RectTransform>();
            wG.lineTemplateY = GraphFrame.transform.GetChild(3).GetComponent<RectTransform>();
            if(stockShown){
                foreach(Stock s in AvailableStocks){
                    if(s.ShownOnGraph){
                        s.ShownOnGraph = false;
                        break;
                    }
                }
            }
            stock.ShownOnGraph=true;
            GameObject.Find("Ticker").GetComponent<TMP_Text>().text = stock.ticker;
            wG.ShowGraph(stock.PriceHistory);
            stockShown = true;
        }
    }
    void UpdateStockPrices(){
        updateStart = DateTime.Now;
        foreach(Stock s in AvailableStocks){
            int dice = updateRnd.Next(0,2);
            if(!s.bearish&&(dice==0||s.bullish)){
                if(s.bullish)
                    s.price+=(float)Math.Round(updateRnd.NextDouble()*3*(priceIncrementMax-priceIncrementMin)+priceIncrementMin, 4);
                s.price+=(float)Math.Round(updateRnd.NextDouble()*(priceIncrementMax-priceIncrementMin)+priceIncrementMin, 4);
            }
            else{
                if(s.bearish)
                    s.price-=(float)Math.Round(updateRnd.NextDouble()*3*(priceIncrementMax-priceIncrementMin)+priceIncrementMin, 4);
                s.price-=(float)Math.Round(updateRnd.NextDouble()*(priceIncrementMax-priceIncrementMin)+priceIncrementMin, 4);
            }
            s.PriceHistory.Add(s.price);
            if(s.ShownOnGraph){
                SetGraphValues(s);
            }
        }
        if(GameObject.Find("Player").GetComponent<PlayerMovement1stPerson>().lapFocus)
            SetPriceText();
    }
    void SetPriceText(){
        if(OwnedStocks.Count>0){
            GameObject OwnedStockFabsParent = GameObject.Find("OwnedStocks");
            if(OwnedStockFabsParent.transform.childCount>0){
                for(int i = 0; i<OwnedStockFabsParent.transform.childCount;i++){
                    OwnedStockSlot s = OwnedStockFabsParent.transform.GetChild(i).GetComponent<OwnedStockSlot>();
                    s.SellPrice.GetComponent<TMP_Text>().text="$"+Math.Round(s.stock.price, 3);
                    s.BuyPrice.GetComponent<TMP_Text>().text="$"+Math.Round(s.stock.price, 3);
                }
            }
        }
    }
     public bool IsPointerOverElement(string layer)
    {
        return IsPointerOverElement(GetEventSystemRaycastResults(), layer);
    }
    ///Returns 'true' if we touched or hovering on Unity UI element.
    public bool IsPointerOverElement(List<RaycastResult> eventSystemRaysastResults, string layer )
    {
        for(int index = 0;  index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults [index];
            if (curRaysastResult.gameObject.layer == LayerMask.NameToLayer(layer)){
                if(layer=="Buy"||layer=="Sell"||layer=="GraphButton"||layer=="BuyMore"){
                    if(curRaysastResult.gameObject.transform.parent.gameObject.layer==LayerMask.NameToLayer(layer)&&layer!="GraphButton"){
                        BuySellStock = curRaysastResult.gameObject.transform.parent.parent.gameObject;
                    }
                    else{
                        BuySellStock = curRaysastResult.gameObject.transform.parent.gameObject;
                    }
                }
                return true;}
        }
        return false;
    }
    public List<RaycastResult> GetEventSystemRaycastResults()
    {   
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position =  Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll( eventData, raysastResults );
        return raysastResults;
    }
}
