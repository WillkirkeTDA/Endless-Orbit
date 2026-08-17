using UnityEngine;

public class PlayerInput : MonoBehaviour
{
   [SerializeField] private float SpeedRotation = 100;
   [SerializeField] private float SpeedImpulse = 5;
  

    Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = transform.GetComponent<Rigidbody2D>();

    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.forward * SpeedRotation * Time.deltaTime);

        if (Input.anyKeyDown)
        {

            rb.AddForce(transform.up * SpeedImpulse, ForceMode2D.Impulse);


        }


    }
   

}
