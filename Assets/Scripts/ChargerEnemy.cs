using UnityEngine;

// GLM 5.1 via Manifest OS (call_id 164) — corrected: instance materials (not static), redundant SetParent removed
public class ChargerEnemy : EnemyBase
{
    public float chargeSpeed       = 14f;
    public float telegraphDuration = 0.8f;
    public float chargeDuration    = 0.6f;
    public float recoverDuration   = 0.4f;
    public float aggroRange        = 10f;

    enum ChargeState { Idle, Telegraph, Charge, Recover }
    ChargeState state = ChargeState.Idle;

    float   stateTimer;
    Vector3 chargeDirection;
    Renderer bodyRenderer;

    Material greyMat, orangeMat, redMat;

    protected override void BuildVisual()
    {
        greyMat   = Mat(new Color(0.5f, 0.5f, 0.5f));
        orangeMat = Mat(new Color(1f, 0.55f, 0f));
        redMat    = Mat(new Color(1f, 0.1f, 0.1f));

        GameObject body = P(PrimitiveType.Cube, "Body", new Vector3(0, 0.45f, 0), new Vector3(0.9f, 0.9f, 0.9f), greyMat);
        bodyRenderer = body.GetComponent<Renderer>();

        GameObject horn = P(PrimitiveType.Cube, "Horn", new Vector3(0, 0.7f, 0.45f), new Vector3(0.2f, 0.2f, 0.55f), Mat(new Color(0.25f, 0.25f, 0.25f)));
        horn.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
    }

    protected override void UpdateBehaviour()
    {
        float dist = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

        switch (state)
        {
            case ChargeState.Idle:
                if (bodyRenderer) bodyRenderer.material = greyMat;
                if (player != null)
                {
                    Vector3 target = player.position + (transform.position - player.position).normalized * 7f;
                    transform.position += (target - transform.position).normalized * speed * Time.deltaTime;
                    if (dist < aggroRange && Time.time >= nextAttack)
                    { state = ChargeState.Telegraph; stateTimer = 0f; }
                }
                break;

            case ChargeState.Telegraph:
                if (bodyRenderer) bodyRenderer.material = orangeMat;
                if (player != null)
                {
                    Vector3 d = player.position - transform.position; d.y = 0;
                    if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d.normalized);
                }
                stateTimer += Time.deltaTime;
                if (stateTimer >= telegraphDuration)
                { chargeDirection = transform.forward; state = ChargeState.Charge; stateTimer = 0f; }
                break;

            case ChargeState.Charge:
                if (bodyRenderer) bodyRenderer.material = redMat;
                transform.position += chargeDirection * chargeSpeed * Time.deltaTime;
                if (player != null && Vector3.Distance(transform.position, player.position) < 1.2f)
                {
                    GunCharacter gc = player.GetComponent<GunCharacter>();
                    if (gc != null) gc.TakeDamage(attackDamage);
                    nextAttack = Time.time + 2f;
                    state = ChargeState.Recover; stateTimer = 0f; break;
                }
                stateTimer += Time.deltaTime;
                if (stateTimer >= chargeDuration) { state = ChargeState.Recover; stateTimer = 0f; }
                break;

            case ChargeState.Recover:
                if (bodyRenderer) bodyRenderer.material = greyMat;
                stateTimer += Time.deltaTime;
                if (stateTimer >= recoverDuration) { state = ChargeState.Idle; stateTimer = 0f; }
                break;
        }
    }
}
