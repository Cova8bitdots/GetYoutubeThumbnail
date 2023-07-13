using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    private static readonly string[] FILENAMES = new string[]
    {
        "default.jpg",
        "mqdefault.jpg",
        "hqdefault.jpg",
        "sddefault.jpg",
        "maxresdefault.jpg",
    };
    
    public enum Quality
    {
        Default,
        Mid,
        Hight,
        HQ,
        FHD
    }

    private Quality currentQuality = Quality.Default;
    private CancellationTokenSource cts = null;
    [SerializeField] private TMP_InputField m_url;
    [SerializeField] private RawImage[] m_thumbnail = null;

    private void Awake()
    {
        Assert.IsNotNull(m_url, "[UIController] m_url is NULL");
        Assert.IsNotNull(m_thumbnail, "[UIController] m_thumbnail is NULL");
    }

    public void OnQualityChanged(int id)
    {
        currentQuality = (Quality)id;
        Debug.Log("CurrentQuality:"+currentQuality);
    }


    public void OnSubmit()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }

        cts = new CancellationTokenSource();
        GetThumbnailAsync(m_url.text, currentQuality, cts.Token).Forget(e=> Debug.LogError(e));
        
    }

    private async UniTask GetThumbnailAsync(string url, Quality quality, CancellationToken token)
    {
        string thumbnail_url = string.Empty;
        try
        {
            string youtubeId = string.Empty;
            if (url.Contains("watch?v="))
            {
                string[] urls = url.Split("=");
                if (urls.Length != 2)
                {
                    throw new Exception("Invalid URL:" + url);
                }

                youtubeId = urls[1];
            }
            else
            {
                string[] urls = url.Split("/");
                youtubeId = urls[urls.Length - 1];
            }
            if(string.IsNullOrEmpty(youtubeId))throw new Exception("Invalid youtubeId:" + youtubeId);

            thumbnail_url = $"https://img.youtube.com/vi/{youtubeId}/{FILENAMES[(int)quality]}";

            Debug.Log("Try Download Thumbnail:"+thumbnail_url);
            
            var www = UnityWebRequestTexture.GetTexture(thumbnail_url);

            await www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                if (m_thumbnail[(int)quality].texture != null)
                {
                    var prev = m_thumbnail[(int)quality].texture;
                    m_thumbnail[(int)quality].texture = null;
                    Destroy(prev);
                }
                m_thumbnail[(int)quality].texture = texture;
            }
            else
            {
                Debug.LogError("Failed to download texture: " + www.error);
            }
        }
        catch (OperationCanceledException e)
        {
            Debug.LogWarning($"Operation was canceled. (URL:{thumbnail_url})");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    
}
