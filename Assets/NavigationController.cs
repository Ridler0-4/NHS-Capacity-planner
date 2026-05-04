using UnityEngine;
using UnityEngine.UIElements;

public class NavigationController : MonoBehaviour
{
    [SerializeField] private UIDocument timetableDoc;
    [SerializeField] private UIDocument dashboardDoc;

    void Start()
    {
        // Hide both first
        timetableDoc.gameObject.SetActive(false);
        dashboardDoc.gameObject.SetActive(false);

        // Then show timetable by default
        ShowTimetable();
    }

    public void ShowTimetable()
    {
        dashboardDoc.gameObject.SetActive(false);
        timetableDoc.gameObject.SetActive(true);

        // Add nav to timetable
        AddNavBar(timetableDoc.rootVisualElement, true);
    }

    public void ShowDashboard()
    {
        timetableDoc.gameObject.SetActive(false);
        dashboardDoc.gameObject.SetActive(true);

        // Add nav to dashboard
        AddNavBar(dashboardDoc.rootVisualElement, false);
    }

    void AddNavBar(VisualElement root, bool timetableActive)
    {
        // Remove old navbar if exists
        var existing = root.Q<VisualElement>("navbar");
        if (existing != null) root.Remove(existing);

        var navbar = new VisualElement();
        navbar.name = "navbar";
        navbar.style.flexDirection = FlexDirection.Row;
        navbar.style.backgroundColor = new StyleColor(new Color(0.12f, 0.47f, 0.71f));
        navbar.style.paddingLeft = 16;
        navbar.style.paddingRight = 16;
        navbar.style.paddingTop = 8;
        navbar.style.paddingBottom = 8;
        navbar.style.position = Position.Absolute;
        navbar.style.top = 0;
        navbar.style.left = 0;
        navbar.style.right = 0;
        navbar.style.height = 40;

        navbar.Add(MakeNavButton("Timetable", timetableActive, () => ShowTimetable()));
        navbar.Add(MakeNavButton("Dashboard", !timetableActive, () => ShowDashboard()));

        root.Add(navbar);

        // Push content down so it clears the navbar
        root.style.paddingTop = 48;
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