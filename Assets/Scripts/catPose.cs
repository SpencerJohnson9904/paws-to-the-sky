using UnityEngine;

public class CatPose : MonoBehaviour
{
    [SerializeField] Transform head;
    [SerializeField] Vector3 headSpread = new Vector3(-30f, 0f, 0f);
    Quaternion headOriginal;
    [Header("Bones")]
    [SerializeField] Transform leftUpLeg;
    [SerializeField] Transform rightUpLeg;
    [SerializeField] Transform spine;
    [SerializeField] Transform tail;

    [Header("Spread Pose Rotations")]
    [SerializeField] Vector3 leftLegSpread = new Vector3(0f, 0f, -45f);
    [SerializeField] Vector3 rightLegSpread = new Vector3(0f, 0f, 45f);
    [SerializeField] Vector3 spineSpread = new Vector3(-20f, 0f, 0f);
    [SerializeField] Vector3 tailSpread = new Vector3(40f, 0f, 0f);

    [Header("Front Legs")]
    [SerializeField] Transform leftShoulder;
    [SerializeField] Transform rightShoulder;
    [SerializeField] Transform leftArm;
    [SerializeField] Transform rightArm;

    [Header("Front Leg Spread Rotations")]
    [SerializeField] Vector3 leftShoulderSpread = new Vector3(0f, 0f, -45f);
    [SerializeField] Vector3 rightShoulderSpread = new Vector3(0f, 0f, 45f);
    [SerializeField] Vector3 leftArmSpread = new Vector3(0f, 0f, -20f);
    [SerializeField] Vector3 rightArmSpread = new Vector3(0f, 0f, 20f);

    Quaternion leftShoulderOriginal;
    Quaternion rightShoulderOriginal;
    Quaternion leftArmOriginal;
    Quaternion rightArmOriginal;

    [SerializeField] float poseSpeed = 5f;

    Rigidbody rb;
    bool isFalling;

    // store original rotations
    Quaternion leftLegOriginal;
    Quaternion rightLegOriginal;
    Quaternion spineOriginal;
    Quaternion tailOriginal;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (leftUpLeg) leftLegOriginal = leftUpLeg.localRotation;
        if (rightUpLeg) rightLegOriginal = rightUpLeg.localRotation;
        if (spine) spineOriginal = spine.localRotation;
        if (tail) tailOriginal = tail.localRotation;
        if (leftShoulder) leftShoulderOriginal = leftShoulder.localRotation;
        if (rightShoulder) rightShoulderOriginal = rightShoulder.localRotation;
        if (leftArm) leftArmOriginal = leftArm.localRotation;
        if (rightArm) rightArmOriginal = rightArm.localRotation;
        if (head) headOriginal = head.localRotation;
    }

    void Update()
    {
        // falling = moving downward and not grounded
        isFalling = rb.linearVelocity.y < -0.5f;

        if (isFalling)
            ApplySpreadPose();
        else
            ResetPose();
    }

    void ApplySpreadPose()
    {
        if (leftUpLeg) leftUpLeg.localRotation = Quaternion.Slerp(
            leftUpLeg.localRotation,
            Quaternion.Euler(leftLegSpread),
            Time.deltaTime * poseSpeed);

        if (rightUpLeg) rightUpLeg.localRotation = Quaternion.Slerp(
            rightUpLeg.localRotation,
            Quaternion.Euler(rightLegSpread),
            Time.deltaTime * poseSpeed);

        if (spine) spine.localRotation = Quaternion.Slerp(
            spine.localRotation,
            Quaternion.Euler(spineSpread),
            Time.deltaTime * poseSpeed);

        if (tail) tail.localRotation = Quaternion.Slerp(
            tail.localRotation,
            Quaternion.Euler(tailSpread),
            Time.deltaTime * poseSpeed);
        if (leftShoulder) leftShoulder.localRotation = Quaternion.Slerp(
    leftShoulder.localRotation, Quaternion.Euler(leftShoulderSpread), Time.deltaTime * poseSpeed);

        if (rightShoulder) rightShoulder.localRotation = Quaternion.Slerp(
            rightShoulder.localRotation, Quaternion.Euler(rightShoulderSpread), Time.deltaTime * poseSpeed);

        if (leftArm) leftArm.localRotation = Quaternion.Slerp(
            leftArm.localRotation, Quaternion.Euler(leftArmSpread), Time.deltaTime * poseSpeed);

        if (rightArm) rightArm.localRotation = Quaternion.Slerp(
            rightArm.localRotation, Quaternion.Euler(rightArmSpread), Time.deltaTime * poseSpeed);
        if (head) head.localRotation = Quaternion.Slerp(
    head.localRotation, Quaternion.Euler(headSpread), Time.deltaTime * poseSpeed);
    }

    void ResetPose()
    {
        if (leftUpLeg) leftUpLeg.localRotation = Quaternion.Slerp(
            leftUpLeg.localRotation, leftLegOriginal, Time.deltaTime * poseSpeed);

        if (rightUpLeg) rightUpLeg.localRotation = Quaternion.Slerp(
            rightUpLeg.localRotation, rightLegOriginal, Time.deltaTime * poseSpeed);

        if (spine) spine.localRotation = Quaternion.Slerp(
            spine.localRotation, spineOriginal, Time.deltaTime * poseSpeed);

        if (tail) tail.localRotation = Quaternion.Slerp(
            tail.localRotation, tailOriginal, Time.deltaTime * poseSpeed);
        if (leftShoulder) leftShoulder.localRotation = Quaternion.Slerp(
    leftShoulder.localRotation, leftShoulderOriginal, Time.deltaTime * poseSpeed);

        if (rightShoulder) rightShoulder.localRotation = Quaternion.Slerp(
            rightShoulder.localRotation, rightShoulderOriginal, Time.deltaTime * poseSpeed);

        if (leftArm) leftArm.localRotation = Quaternion.Slerp(
            leftArm.localRotation, leftArmOriginal, Time.deltaTime * poseSpeed);

        if (rightArm) rightArm.localRotation = Quaternion.Slerp(
            rightArm.localRotation, rightArmOriginal, Time.deltaTime * poseSpeed);
        if (head) head.localRotation = Quaternion.Slerp(
            head.localRotation, headOriginal, Time.deltaTime * poseSpeed);
    }
}