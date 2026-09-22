namespace Unity.Tutorials.Core.Editor
{
    public class View
    {
        internal virtual string Name => string.Empty;
        protected TutorialWindow Application => TutorialWindow.Instance;
        public View() { }

        public virtual void SubscribeEvents() { }
        public virtual void UnubscribeEvents() { }
    }
}
