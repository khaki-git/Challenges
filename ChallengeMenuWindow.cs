namespace Challenges
{
    public class ChallengeMenuWindow: MenuWindow
    {
        public override bool openOnStart => false;
        public override bool selectOnOpen => true;
        public override bool closeOnPause => false;
        public override bool closeOnUICancel => true;
        public override bool autoHideOnClose => false;
    }
}