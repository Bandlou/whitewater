using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    // PUBLIC FIELDS
    public WaterManager waterManager;
    public Color underwaterFogColor = new Color(0, 0.4f, 0.7f, 0.6f);
    public float underwaterFogDensity = .2f;

    // PRIVATE FIELDS
    private bool defaultFog;
    private Color defaultFogColor;
    private float defaultFogDensity;
    private Material defaultSkybox;
    private Material underwaterSkybox;

    // LIFECYCLE

    private void Awake()
    {
        defaultFog = RenderSettings.fog;
        defaultFogColor = RenderSettings.fogColor;
        defaultFogDensity = RenderSettings.fogDensity;
        defaultSkybox = RenderSettings.skybox;
        underwaterSkybox = null;
    }

    void Start()
    {
        GetComponent<Camera>().backgroundColor = new Color(0, 0.4f, 0.7f, 1);
    }

    void Update()
    {
        // Underwater effect
        if (transform.position.y < waterManager.transform.position.y)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = underwaterFogColor;
            RenderSettings.fogDensity = underwaterFogDensity;
            RenderSettings.skybox = underwaterSkybox;
        }
        else
        {
            RenderSettings.fog = defaultFog;
            RenderSettings.fogColor = defaultFogColor;
            RenderSettings.fogDensity = defaultFogDensity;
            RenderSettings.skybox = defaultSkybox;
        }
    }
}
