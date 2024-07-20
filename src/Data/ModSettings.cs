namespace RoboticInbox.Data
{
    internal class ModSettings
    {
        /// <summary>
        /// How wide to scan for storage containers to distribute to.
        /// </summary>
        /// <remarks>more impactful than vertical range; this value should be synced to each player via 'roboticInboxRangeH' cvar</remarks>
        public int InboxHorizontalRange { get; set; } = 5;

        /// <summary>
        /// How low/high to scan for storage containers to distribute to.
        /// </summary>
        /// <remarks>less impactful than horizontal range; this value should be synced to each player via 'roboticInboxRangeV' cvar</remarks>
        public int InboxVerticalRange { get; set; } = 5;

        /// <summary>
        /// How long to leave successful distribution notification up on boxes before reverting them.
        /// </summary>
        /// <remarks>limiting this is good; if the server shuts down while a message is up, it could get stuck that way</remarks>
        public float DistributionSuccessNoticeTime { get; set; } = 2f;

        /// <summary>
        /// How long to leave blocked distribution notification up on boxes before reverting them.
        /// </summary>
        /// <remarks>limiting this is good; if the server shuts down while a message is up, it could get stuck that way</remarks>
        public float DistributionBlockedNoticeTime { get; set; } = 3f;

        /// <summary>
        /// Whether inboxes within claimed land should prevent scanning outide the bounds of their lcb.
        /// </summary>
        /// <remarks>this helps to prevent pvp players from setting up hidden container 'siphons' near a base without the base owner immediately realizing that some/all items are being pulled out<remarks>
        public bool BaseSiphoningProtection { get; set; } = true;

        /// <summary>
        /// Whether inboxes outside claimed land should be blocked from scanning containers within claimed land (or other land claims).
        /// </summary>
        /// <remarks>this helps to prevent raiders from using inboxes from outside claimed land to listen for lock sounds within a nearby base</remarks>
        // public bool BaseFishingProtection { get; set; } = true; // TODO: implement
    }
}
