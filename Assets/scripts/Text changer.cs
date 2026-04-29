using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Textchanger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    double capacityLevel = 100;
    [SerializeField]
    TextMeshProUGUI Status;

    void Start()
    {
        Status.text = "Standard";
    }

    // Update is called once per frame
    void Update()
    {
        capacityLevel = capacityLevel - 0.01;
        if (capacityLevel <= 90)
        {
            Status.text = "Intervention necessary, capacity under 90%";
        }
        else if (capacityLevel <= 95)
        {
            Status.text = "Warning, Capacity under 95%";
        }
        else
        {
            Status.text = "Normal";
        }
    }
}
