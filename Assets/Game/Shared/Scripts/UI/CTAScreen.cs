using HyperCasual.Core;

public class CTAScreen : View
{
    private void OnDisable()
    {
        //This is a dirty solution for displaying the HUD after the CTA is gone
        //Another possible solution is making this class not inherit from view and just activate the CTA screen normally as a gameobject

        UIManager.Instance.GetView<HyperCasual.Gameplay.Hud>().Show();
    }
}
