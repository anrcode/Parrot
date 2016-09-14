using System;


namespace Parrot.DroneControl
{
    public enum VideoCodec
    {
        Null = 0,
        Uvlc = 32,
        P264 = 64
    }

    public enum VideoBitrateCtlMode
    {
        Disabled = 0,
        Dynamic,
        Manual
    }

    /// <summary>
    /// Identifies which camera captures the images.
    /// </summary>
    public enum VideoChannel
    {
        Horizontal = 0,
        Vertical,
        VerticalInHorizontal,
        HorizontalInVertical,
        Next
    }

    /// <summary>
    /// Indicates the LED animation to perform.
    /// </summary>
    public enum LedAnimation
    {
        BlinkGreenRed = 0,
        BlinkGreen,
        BlinkRed,
        BlinkOrange,
        SnakeGreenRed,
        Fire,
        Standard,
        Red,
        Green,
        RedSnake,
        Blank,
        RightMissile,
        LeftMissile,
        DoubleMissile
    }

    /// <summary>
    /// Identitifies whether the ARDrone detects predetermined coloured patterns.
    /// </summary>
    public enum DetectionType
    {
        VisionDetect = 2,       // starts detection
        None,                   // stops detection
        Cocarde,                // Detects a roundel under the drone 
        OrientedCocarde,        // Detects an oriented roundel under the drone 
        Stripe,                 // Detects a uniform stripe on the ground
        HCorcade,               // Detects a roundel in front of the drone
        HOrientedCocarde,       // Detects an oriented roundel in front of the drone
        StripeV,
        Multiple                // The drone uses several detections at the same time    
    }

    public enum EnemyColor
    {
        OrangeGreen = 1,
        OrangeYellow,
        OrangeBlue,
        ArraceFinishLine = 16,
        ArraceDonut = 17
    }

    public enum TagType : int
    {
        None = 0,
        Shell,
        Roundel,
        OrientedRoundel,
        Stripe,
        Num
    }

    public enum TagDetectSource : int
    {
        CamHorizontal = 0,          // Tag was detected on the front camera picture
        CamVertical,                // Tag was detected on the vertical camera picture at full speed
        CamVerticalHSync,           // Tag was detected on the vertical camera picture inside the horizontal pipeline
    }

    public enum FlightAnimation
    {
        PhiM30Deg = 0,
        Phi30Deg,
        ThetaM30Deg,
        Theta30Deg,
        Theta20DegYaw200Deg,
        Theta20DegYawM200Deg,
        Turnaround,
        TurnaroundGoDown,
        YawShake,
        YawDance,
        PhiDance,
        ThetaDance,
        VzDance,
        Wave,
        PhiThetaMixed,
        DoublePhiThetaMixed
    }
}
