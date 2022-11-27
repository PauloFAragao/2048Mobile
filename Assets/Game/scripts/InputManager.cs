using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] GameManager gm;

    private Vector2 touchStartPosition;//posição inicial do input na tela

    private bool control;//variável que vai evitar mais de um comando por input

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);

            switch (t.phase)
            {
                case TouchPhase.Began:
                    control = true;

                    touchStartPosition = t.position;
                    break;

                case TouchPhase.Moved:
                    if (control)
                    {
                        if (Mathf.Abs(t.deltaPosition.x) > 10 || Mathf.Abs(t.deltaPosition.y) > 10)
                        {
                            if (Mathf.Abs(touchStartPosition.x - t.position.x) > Mathf.Abs(touchStartPosition.y - t.position.y))
                            {
                                control = false;

                                if (touchStartPosition.x > t.position.x)//direita para esquerda
                                    gm.Shift(Vector2.left);

                                else//da esquerda para a direita
                                    gm.Shift(Vector2.right);
                            }
                            else if (Mathf.Abs(touchStartPosition.x - t.position.x) != Mathf.Abs(touchStartPosition.y - t.position.y))
                            {
                                control = false;

                                if (touchStartPosition.y > t.position.y)//de cima para baixo
                                    gm.Shift(Vector2.down);

                                else//de baixo para cima
                                    gm.Shift(Vector2.up);
                            }
                        }
                    }
                    break;

                default:
                    return;
            }

        }

    }

}
