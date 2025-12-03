using System;
using Unity.Netcode;

[System.Serializable]
public class DeckCardsResponse
{
    public string status;
    public DeckCardDto[] cards;
}

[System.Serializable]
public struct DeckCardDto : INetworkSerializable
{
    public int card_id;
    public string card_uid;
    public string name;
    public string type;

    public int cost;
    public int health;
    public int attack;
    public string rarity;
    public string rules_text;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        //Replacing the nulls before they're serialized. This has been such a headache.
        if (card_uid == null) card_uid = "";
        if (name == null) name = "";
        if (rarity == null) rarity = "";
        if (rules_text == null) rules_text = "";

        serializer.SerializeValue(ref card_id);
        serializer.SerializeValue(ref card_uid);
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref cost);
        serializer.SerializeValue(ref health);
        serializer.SerializeValue(ref attack);
        serializer.SerializeValue(ref rarity);
        serializer.SerializeValue(ref rules_text);
    }
}