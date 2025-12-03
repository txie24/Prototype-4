using UnityEngine;
using System.Collections.Generic;

public class ShipDeckInventory : MonoBehaviour
{
    [Header("Debug Info")]
    public List<FuelItem> fuelOnDeck = new List<FuelItem>();

    public int FuelCount => fuelOnDeck.Count;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Fuel"))
        {
            FuelItem item = other.GetComponent<FuelItem>();
            if (item != null && !fuelOnDeck.Contains(item))
            {
                fuelOnDeck.Add(item);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Fuel"))
        {
            FuelItem item = other.GetComponent<FuelItem>();
            if (item != null && fuelOnDeck.Contains(item))
            {
                fuelOnDeck.Remove(item);
            }
        }
    }
    public void ConsumeOneFuel()
    {
        if (fuelOnDeck.Count > 0)
        {
            FuelItem itemToRemove = fuelOnDeck[0];
            fuelOnDeck.RemoveAt(0);

            if (itemToRemove != null)
                Destroy(itemToRemove.gameObject);
        }
    }
}