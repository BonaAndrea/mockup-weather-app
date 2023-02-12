using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public struct Weather
{
    public int temperature;
    public int feels_like;
    public string main;
    public string description;
    public string icon;
}

[CreateAssetMenu(fileName = "WeatherData", menuName = "Weather Data/Create Weather Data", order = 1)]
public class WeatherData : ScriptableObject
{
    public Weather[] weatherData;

    public void RandomizeValues()
    {
        for(int i=0; i<weatherData.Length; i++)
        {
            weatherData[i].temperature = Random.Range(-20, 40);
            weatherData[i].feels_like = Random.Range(-20, 40);
        }
    }
    
    
    
}

