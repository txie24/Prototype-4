using UnityEngine;
using TMPro;

[DefaultExecutionOrder(10)]
public class LeverTextAnchor : MonoBehaviour
{
    [Header("Display")]
    [TextArea] public string prompt = "Press F to pull";
    public float showRadius = 3f;
    public Vector3 worldOffset = new Vector3(0, 1.2f, 0);
    public bool hideWhenBehindCamera = true;

    [Header("Target search")]
    [Tooltip("Tags to search for a target at runtime. First found wins.")]
    public string[] tagsToSearch = new[] { "Player" };

    [Header("Style")]
    public float fontSize = 2.2f;
    public float fadeSpeed = 12f;

    Camera cam;
    Canvas worldCanvas;
    RectTransform rt;
    TextMeshProUGUI tmp;

    Transform target;
    float targetAlpha = 0f;
    float currentAlpha = 0f;
    float retryTimer = 0f;

    void Awake()
    {
        cam = Camera.main;

        // create world-space canvas as a child of this object
        var canvasGO = new GameObject("__LeverPromptCanvas");
        canvasGO.layer = gameObject.layer;
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = worldOffset;

        worldCanvas = canvasGO.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 400;

        rt = worldCanvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2.5f, 1f);

        // create text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform, false);
        tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.fontSize = fontSize;
        tmp.text = prompt;
        tmp.color = new Color(1, 1, 1, 0);

        var textRT = tmp.GetComponent<RectTransform>();
        textRT.anchorMin = textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;

        HideImmediate();
    }

    void LateUpdate()
    {
        if (!cam) cam = Camera.main;

        // try to find player if we don't have one yet
        if (!target)
        {
            retryTimer -= Time.unscaledDeltaTime;
            if (retryTimer <= 0f)
            {
                ResolveTarget();
                retryTimer = 0.5f;
            }
        }

        if (!worldCanvas) return;

        // face camera
        if (cam)
        {
            worldCanvas.transform.rotation =
                Quaternion.LookRotation(worldCanvas.transform.position - cam.transform.position);
        }

        // stick to lever position + offset
        worldCanvas.transform.position = transform.position + worldOffset;

        // visibility based on distance
        bool shouldShow = false;
        if (target)
        {
            float dist = Vector3.Distance(target.position, transform.position);
            shouldShow = dist <= showRadius;
        }

        // hide if behind camera
        if (hideWhenBehindCamera && cam)
        {
            Vector3 toObj = (worldCanvas.transform.position - cam.transform.position).normalized;
            if (Vector3.Dot(cam.transform.forward, toObj) <= 0f)
                shouldShow = false;
        }

        targetAlpha = shouldShow ? 1f : 0f;

        // smooth fade
        currentAlpha = Mathf.MoveTowards(
            currentAlpha,
            targetAlpha,
            fadeSpeed * Time.unscaledDeltaTime
        );

        var c = tmp.color;
        c.a = currentAlpha;
        tmp.color = c;

        bool enabled = currentAlpha > 0.001f;
        if (worldCanvas.enabled != enabled)
            worldCanvas.enabled = enabled;
    }

    void ResolveTarget()
    {
        if (tagsToSearch == null) return;

        for (int i = 0; i < tagsToSearch.Length; i++)
        {
            string tag = tagsToSearch[i];
            if (string.IsNullOrEmpty(tag)) continue;
            var go = GameObject.FindGameObjectWithTag(tag);
            if (go)
            {
                target = go.transform;
                return;
            }
        }
    }

    void OnDisable() => HideImmediate();

    void HideImmediate()
    {
        currentAlpha = 0f;
        if (tmp)
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 0f);
        if (worldCanvas)
            worldCanvas.enabled = false;
    }
}
