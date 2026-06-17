using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    //Mirar con la cámara
    public Transform camara;
    float horizontalVel = 1000;
    float verticalVel = 1000;

    float horizontal;
    float vertical;

    float verticalAux;

    //Movimiento del personaje
    public CharacterController characterController;
    [SerializeField] float velocidad = 4f;
    float velocidadCalculada;
    public float frenada;
    float x, z;
    public Vector3 mover;

    //Gravedad en personaje
    Vector3 moverVertical;
    [SerializeField] float gravedad = -15f;
    bool tocaSuelo;

    //Saltar
    [SerializeField] float fuerzaSalto = 15f;
    float valorSalto;

    public float fuerzaEmpuje = 8;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        frenada = 0f;        

        //Establece una relación entre la fuerza del salto y la gravedad
        valorSalto = Mathf.Sqrt(fuerzaSalto * -2 * gravedad);
    }

    void Update()
    {
        velocidadCalculada = velocidad - frenada;

        Mirar();
        if (tocaSuelo)
        {
            moverVertical.y = gravedad;
        }
        Saltar();
        Mover();   
        
        moverVertical.y += gravedad * Time.deltaTime;
        characterController.Move(moverVertical * Time.deltaTime);
    }

    void Saltar()
    {
        if (Input.GetButtonDown("Jump"))
        {
            moverVertical.y = valorSalto;
            tocaSuelo = false;
        }
    }
    void Mover()
    {
        x = Input.GetAxis("Horizontal");
        z = Input.GetAxis("Vertical");

        mover = x * transform.right + z * transform.forward;
        characterController.Move(mover * velocidadCalculada * Time.deltaTime);


    }
    void Mirar()
    {
        horizontal = Input.GetAxis("Mouse X") * horizontalVel * Time.deltaTime;
        vertical = Input.GetAxis("Mouse Y") * verticalVel * Time.deltaTime;

        transform.Rotate(0, horizontal, 0);

        verticalAux -= vertical;
        verticalAux = Mathf.Clamp(verticalAux, -90f, 90f);

        camara.localRotation = Quaternion.Euler(verticalAux, 0, 0);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Walkeable"))
            tocaSuelo = true;
    }   
}
