[System.Serializable]
public class DeckCardsResponse
{
    public string status;
    public DeckCardDto[] cards;
}

[System.Serializable]
public class DeckCardDto
{
    public int card_id;
    public string card_uid;
    public string name;
    public int cost;
    public int health;
    public int attack;
    public string rarity;
    public string rules_text;
}
