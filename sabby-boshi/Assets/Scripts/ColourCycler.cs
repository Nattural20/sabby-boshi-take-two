using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColourCycler : MonoBehaviour
{
    private Text colourText;
    private Image colourImage;
    private TMP_Text colourTextTMP;
    private SpriteRenderer colourSprite;
    [SerializeField] private float cycleSpeed;
    [SerializeField] private bool isImage;
    [SerializeField] private bool isTMP;
    [SerializeField] private bool isSprite;
    
    private float hue;
    private float sat;
    private float vib;

    private void Start()
    {
        if (!isImage && !isTMP && !isSprite)
            colourText = transform.GetComponent<Text>();
        else if (isTMP)
            colourTextTMP = transform.GetComponent<TMP_Text>();
        else if (isImage)
            colourImage = transform.GetComponent<Image>();
        else if (isSprite)
            colourSprite = transform.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!isImage && !isTMP && !isSprite)
            CycleColour();
        else if (isTMP)
            CycleTMPColour();
        else if (isImage)
            CycleColourImage();
        else if (isSprite)
            CycleColourSprite();
    }

    private void CycleColour()
    {
        Color.RGBToHSV(colourText.color, out hue, out sat, out vib);
        hue += cycleSpeed / 10000;
        if(hue >= 1)
        {
            hue = 0;
        }

        sat = 1;
        vib = 1;
        colourText.color = Color.HSVToRGB(hue, sat, vib);
    }

    private void CycleColourImage()
    {
        Color.RGBToHSV(colourImage.color, out hue, out sat, out vib);
        hue += cycleSpeed / 10000;
        if (hue >= 1)
        {
            hue = 0;
        }

        sat = 1;
        vib = 1;
        colourImage.color = Color.HSVToRGB(hue, sat, vib);
    }

    private void CycleColourSprite()
    {
        Color.RGBToHSV(colourSprite.color, out hue, out sat, out vib);
        hue += cycleSpeed / 10000;
        if (hue >= 1)
        {
            hue = 0;
        }

        sat = .5f;
        vib = 1;
        colourSprite.color = Color.HSVToRGB(hue, sat, vib);
    }

    private void CycleTMPColour()
    {
        Color.RGBToHSV(colourTextTMP.color, out hue, out sat, out vib);
        hue += cycleSpeed / 10000;
        if (hue >= 1)
        {
            hue = 0;
        }

        sat = 1;
        vib = 1;
        colourTextTMP.color = Color.HSVToRGB(hue, sat, vib);
    }
}
