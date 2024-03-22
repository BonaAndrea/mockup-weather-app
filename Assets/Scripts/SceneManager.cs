using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;
using UnityEngine.Events;

public class SceneManager : MonoBehaviour
{
    [SerializeField] private string _APIKey = "20f5ff1300c69842c8f2b5a2028fbbac";
    [SerializeField] private float _lat = 45.062884f, _lon = 7.679101f;
    [SerializeField] private int _hoursToShow = 24;
    [SerializeField] private float _animationSpeed = 0.1f;
    [SerializeField] private float _iconAnimationDuration = 0.5f;
    [SerializeField] private float _textAnimationDuration = 0.5f;
    [SerializeField] private TextMeshProUGUI _currentTemperature;
    private int _currentTempValue;
    [SerializeField] private TextMeshProUGUI _weatherDescription;
    [SerializeField] private TextMeshProUGUI _minMaxTemp;
    private Vector2 _minMaxTempValue;
    [SerializeField] private TextMeshProUGUI _dateTimeText;
    [SerializeField] private TextMeshProUGUI _feels_like;
    private int _feelsLikeValue;
    private int _currentHour;
    [SerializeField] private Image _bigIcon;
    [SerializeField] private CanvasGroup _bigIconCanvasGroup;

    [SerializeField] private WeatherData _weatherData;

    [SerializeField] private Transform _content;
    [SerializeField] private GameObject _hourWeather;

    private HourlyIcon[] _hourlyIcons;

    private string _imageUrlPrefix = "http://openweathermap.org/img/wn/";


    private void Start()
    {
        Application.targetFrameRate = 60;
        _hourlyIcons = new HourlyIcon[_hoursToShow];
        SetIconsAndText();
        SetMainText();
    }

    private void InitializeHourWeather()
    {
        bool shouldInstantiate = false;
        if (_content.childCount != _hoursToShow) shouldInstantiate = true;
        int minTemp, maxTemp;
        (minTemp, maxTemp) = GetMinMaxTemp();
        StartCoroutine(ChangeValueOverTime(_minMaxTempValue, new Vector2(minTemp, maxTemp), _textAnimationDuration,
            (Vector2 value) => _minMaxTemp.text =
                "Massima: " + value.y.ToString() + "°C - Minima: " + value.x.ToString() + "°C"));
        float temperatureRange = maxTemp - minTemp;
        int currentHour = DateTime.Now.Hour;
        StartCoroutine(UpdateHourlyIcons(shouldInstantiate, currentHour, minTemp, temperatureRange));
        
    }

    private IEnumerator UpdateHourlyIcons(bool shouldInstantiate, int currentHour, int minTemp, float temperatureRange)
    {
        for (int i = 0; i < _hoursToShow; i++)
        {
            if (shouldInstantiate)
            {
                _hourlyIcons[i] = Instantiate(_hourWeather, _content).GetComponent<HourlyIcon>();
            }

            StartCoroutine(UpdateHourlyIconValues(_hourlyIcons[i], ((currentHour + i) % 24),
                _weatherData.weatherData[i], minTemp, temperatureRange));

        }

        yield return null;
    }

    private IEnumerator UpdateHourlyIconValues(HourlyIcon _hourlyIcon, int newTime, Weather weatherData, int minTemp, float temperatureRange)
    {
        StartCoroutine(ChangeValueOverTime(_hourlyIcon.Hour, newTime, _textAnimationDuration, (int value) =>
        {
            _hourlyIcon.Hour = value;
            _hourlyIcon.HourText.text = value.ToString("D2") + ":00";
        }));
        StartCoroutine(ChangeValueOverTime(_hourlyIcon.Temperature, weatherData.temperature, _textAnimationDuration, (int value) =>
        {
            _hourlyIcon.Temperature = value;
            _hourlyIcon.HourTempText.text = value.ToString() + "°C";
        }));
        StartCoroutine(DownloadImage(CreateSpriteFromTexture, weatherData.icon + "@2x.png",
            _hourlyIcon.Icon, _hourlyIcon.CanvasGroup));
        StartCoroutine(UpdateHourlyColumn(_hourlyIcon.Column.rectTransform,
            weatherData.temperature, minTemp, temperatureRange));
        yield return null;
    }
    
    private (int minTemp, int maxTemp) GetMinMaxTemp()
    {
        int minTemp = Int32.MaxValue;
        int maxTemp = Int32.MinValue;
        foreach (Weather w in _weatherData.weatherData)
        {
            if (w.temperature < minTemp)
            {
                minTemp = w.temperature;
                continue;
            }
            else if (w.temperature > maxTemp)
            {
                maxTemp = w.temperature;
                continue;
            }
        }

        return (minTemp, maxTemp);
    }

    private IEnumerator DownloadImage(Action<Texture2D, Image, CanvasGroup> callback, string imageUrl,
        Image spriteToAssign, CanvasGroup canvasGroup)
    {
        imageUrl = _imageUrlPrefix + imageUrl;
        Debug.Log(imageUrl);
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return www.SendWebRequest();

        if (www.error != null)
        {
            Debug.LogError("Error downloading image: " + www.error);
        }
        else
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            callback(texture, spriteToAssign, canvasGroup);
        }
    }

    private void CreateSpriteFromTexture(Texture2D texture, Image spriteToAssign, CanvasGroup canvasGroup)
    {
        if (texture == null)
        {
            return;
        }

        Sprite sprite = Sprite.Create(texture,
            new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        StartCoroutine(ChangeSprite(canvasGroup, spriteToAssign, sprite));
        //spriteToAssign.sprite = sprite;
    }

    void Update()
    {
        _dateTimeText.text = DateTime.Now.ToString("dd MMMM, HH:mm");
    }


    private void SetIconsAndText()
    {
        InitializeHourWeather();
        StartCoroutine(DownloadImage(CreateSpriteFromTexture, _weatherData.weatherData[0].icon + "@4x.png", _bigIcon,
            _bigIconCanvasGroup));
    }

    private IEnumerator SetDownloadedIconsAndText()
    {
        yield return StartCoroutine(GetWeatherData());
        SetMainText();
        yield return StartCoroutine(GetHourlyData());
        InitializeHourWeather();
        StartCoroutine(DownloadImage(CreateSpriteFromTexture, _weatherData.weatherData[0].icon + "@4x.png", _bigIcon,
            _bigIconCanvasGroup));
    }

    private void SetMainText()
    {
        StartCoroutine(ChangeValueOverTime(_currentTempValue, _weatherData.weatherData[0].temperature, _textAnimationDuration, (int value) =>
        {
            _currentTemperature.text = value.ToString() + "°C";
        }));
        _weatherDescription.text = _weatherData.weatherData[0].description;
        StartCoroutine(ChangeValueOverTime(_feelsLikeValue, _weatherData.weatherData[0].temperature, _textAnimationDuration, (int value) => _feels_like.text = "Percepita: " + value.ToString() + "°C"));
    }

    //EXTRA 1A
    public void RandomizeValues()
    {
        StopAllCoroutines();
        _weatherData.RandomizeValues();
        SetMainText();
        SetIconsAndText();
    }

    //EXTRA 1B
    public void GetNewValues()
    {
        StopAllCoroutines();

        StartCoroutine(SetDownloadedIconsAndText());
    }

    private IEnumerator GetWeatherData()
    {
        string url = "http://api.openweathermap.org/data/2.5/weather?lat=" + _lat + "&lon=" + _lon + "&appid=" +
                     _APIKey + "&units=metric&lang=IT";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.error != null)
        {
            Debug.LogError("Error getting current weather data: " + request.error);
        }
        else
        {
            string response = request.downloadHandler.text;
            JSONNode data = JSON.Parse(response);
            Debug.Log(response);
            _weatherData.weatherData[0].main = (string)data["weather"][0]["main"];
            _weatherData.weatherData[0].description = data["weather"][0]["description"];
            char firstLetter = _weatherData.weatherData[0].description[0];
            _weatherData.weatherData[0].description =
                char.ToUpper(firstLetter) + _weatherData.weatherData[0].description.Substring(1);
            _weatherData.weatherData[0].icon = (string)data["weather"][0]["icon"];
            Debug.Log(_weatherData.weatherData[0].icon);
            _weatherData.weatherData[0].temperature = (int)data["main"]["temp"].AsFloat;
            Debug.Log(_weatherData.weatherData[0].temperature);
            _weatherData.weatherData[0].feels_like = data["main"]["feels_like"].AsInt;
        }
    }

    private IEnumerator GetHourlyData()
    {
        string url = "https://pro.openweathermap.org/data/2.5/forecast/hourly?lat=" + _lat + "&lon=" + _lon +
                     "&appid=" + _APIKey + "&units=metric&lang=IT";
        UnityWebRequest www = UnityWebRequest.Get(url);

        yield return www.SendWebRequest();

        if (www.error != null)
        {
            Debug.LogError("Error getting hourly weather data: " + www.error);
        }
        else
        {
            JSONNode data = JSON.Parse(www.downloadHandler.text);
            for (int i = 1; i < 24; i++)
            {
                _weatherData.weatherData[i].main = data["list"][i]["weather"][0]["main"];
                _weatherData.weatherData[i].description = data["list"][i]["weather"][0]["description"];
                _weatherData.weatherData[i].icon = data["list"][i]["weather"][0]["icon"].Value + "@2x.png";
                Debug.Log(_weatherData.weatherData[i].icon);
                _weatherData.weatherData[i].temperature = data["list"][i]["main"]["temp"].AsInt;
                _weatherData.weatherData[i].feels_like = data["list"][i]["main"]["feels_like"].AsInt;
            }
        }
    }

    //EXTRA 2A
    private IEnumerator UpdateHourlyColumn(RectTransform toModify, float temperature, float minTemp,
        float temperatureRange)
    {
        float fromHeight = toModify.sizeDelta.y;
        float toHeight = Mathf.Lerp(50, 300, (temperature - minTemp) / temperatureRange);
        float elapsedTime = 0;

        while (elapsedTime < 1)
        {
            elapsedTime += Time.deltaTime * _animationSpeed;
            toModify.sizeDelta = new Vector2(toModify.sizeDelta.x, Mathf.Lerp(fromHeight, toHeight, elapsedTime));
            yield return null;
        }
    }

    
    //EXTRA 2B
    private IEnumerator ChangeSprite(CanvasGroup cg, Image image, Sprite newSprite)
    {
        // Fade out the old sprite
        float time = 0f;
        while (time < _iconAnimationDuration)
        {
            time += Time.deltaTime;
            cg.alpha = 1f - time / _iconAnimationDuration;
            yield return null;
        }

        // Change the sprite
        image.sprite = newSprite;

        // Fade in the new sprite
        time = 0f;
        while (time < _iconAnimationDuration)
        {
            time += Time.deltaTime;
            cg.alpha = time / _iconAnimationDuration;
            yield return null;
        }
    }
    
    private IEnumerator ChangeValueOverTime(int start, int end, float duration, UnityAction<int> updateAction)
    {
        float elapsedTime = 0f;
        float current = start;
        float velocity = 3f;
        while (current < end)
        {
            // V = S/T
            // S = V * T
            //current = Mathf.MoveTowards(current, end, Time.deltaTime * velocity);
            current += Time.deltaTime * velocity * Mathf.Sign(end-start);
            //int currentValue = Mathf.RoundToInt(Mathf.Lerp(start, end, elapsedTime / duration));
            updateAction(Mathf.RoundToInt(current));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        updateAction(end);
    }
    private IEnumerator ChangeValueOverTime(Vector2 startValue, Vector2 endValue, float duration, UnityAction<Vector2> updateAction)
    {float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float currentValueX = Mathf.RoundToInt(Mathf.Lerp(startValue.x, endValue.x, elapsedTime / duration));
            float currentValueY = Mathf.RoundToInt(Mathf.Lerp(startValue.y, endValue.y, elapsedTime / duration));
            updateAction(new Vector2(currentValueX, currentValueY));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        updateAction(endValue);
    }

    



}