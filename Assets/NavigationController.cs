using UnityEngine;
using UnityEngine.UIElements;

public class NavigationController : MonoBehaviour
{
    [SerializeField] private UIDocument timetableDoc;
    [SerializeField] private UIDocument dashboardDoc;
    [SerializeField] private UIDocument manageDoc;
    [SerializeField] private Sprite logo;

    void Start()
    {
        timetableDoc.gameObject.SetActive(false);
        dashboardDoc.gameObject.SetActive(false);
        manageDoc.gameObject.SetActive(false);

        ShowTimetable();
    }

    public void ShowTimetable()
    {
        dashboardDoc.gameObject.SetActive(false);
        manageDoc.gameObject.SetActive(false);
        timetableDoc.gameObject.SetActive(true);
        AddNavBar(timetableDoc.rootVisualElement, 0);
    }

    public void ShowDashboard()
    {
        timetableDoc.gameObject.SetActive(false);
        manageDoc.gameObject.SetActive(false);
        dashboardDoc.gameObject.SetActive(true);
        AddNavBar(dashboardDoc.rootVisualElement, 1);
    }

    public void ShowManage()
    {
        timetableDoc.gameObject.SetActive(false);
        dashboardDoc.gameObject.SetActive(false);
        manageDoc.gameObject.SetActive(true);
        AddNavBar(manageDoc.rootVisualElement, 2);
    }

    void AddNavBar(VisualElement root, int activeIndex)
    {
        var existing = root.Q<VisualElement>("navbar");
        if (existing != null) root.Remove(existing);

        var navbar = new VisualElement();
        navbar.name = "navbar";
        navbar.style.flexDirection = FlexDirection.Row;
        navbar.style.backgroundColor = new StyleColor(new Color(0f, 0.369f, 0.722f));
        navbar.style.paddingLeft = 16;
        navbar.style.paddingRight = 16;
        navbar.style.paddingTop = 8;
        navbar.style.paddingBottom = 8;
        navbar.style.position = Position.Absolute;
        navbar.style.top = 0;
        navbar.style.left = 0;
        navbar.style.right = 0;
        navbar.style.height = 40;

        navbar.Add(MakeNavButton("Timetable", activeIndex == 0, () => ShowTimetable()));
        navbar.Add(MakeNavButton("Dashboard", activeIndex == 1, () => ShowDashboard()));
        navbar.Add(MakeNavButton("Manage", activeIndex == 2, () => ShowManage()));

        root.Add(navbar);
        root.style.paddingTop = 48;
        if (logo != null)
        {
            var logoImg = new Image();
            logoImg.sprite = logo;
            logoImg.style.width = 100;
            logoImg.style.height = 35;
            logoImg.style.marginLeft = StyleKeyword.Auto;
            logoImg.style.marginLeft = StyleKeyword.Auto;
            logoImg.style.marginTop = -4;
            navbar.Add(logoImg);

        }
    }

    VisualElement MakeNavButton(string text, bool active, System.Action onClick)
    {
        var btn = new Button(onClick);
        btn.text = text;
        btn.style.backgroundColor = new StyleColor(Color.clear);
        btn.style.borderTopWidth = 0;
        btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth = 0;
        btn.style.borderRightWidth = 0;
        btn.style.color = new StyleColor(active
            ? Color.white
            : new Color(1f, 1f, 1f, 0.6f));
        btn.style.fontSize = 14;
        btn.style.marginRight = 8;
        btn.style.unityFontStyleAndWeight = active
            ? new StyleEnum<FontStyle>(FontStyle.Bold)
            : new StyleEnum<FontStyle>(FontStyle.Normal);
        return btn;
    }
}