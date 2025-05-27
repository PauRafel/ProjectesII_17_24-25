using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SwipeController : MonoBehaviour, IEndDragHandler
{
    [Header("Page Configuration")]
    [SerializeField] private int maxPage;
    [SerializeField] private Vector3 pageStep;
    [SerializeField] private RectTransform levelPagesRect;

    [Header("Animation Settings")]
    [SerializeField] private float tweenTime;
    [SerializeField] private LeanTweenType tweenType;

    [Header("Navigation Buttons")]
    [SerializeField] private Button previousBtn;
    [SerializeField] private Button nextBtn;

    private int currentPage;
    private Vector3 targetPos;
    private float dragThreshold;

    private const int FIRST_PAGE = 1;
    private const float DRAG_THRESHOLD_DIVISOR = 15f;

    private void Awake()
    {
        InitializeController();
    }

    public void Next()
    {
        if (CanNavigateNext())
        {
            NavigateToNextPage();
        }
    }

    public void Previous()
    {
        if (CanNavigatePrevious())
        {
            NavigateToPreviousPage();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ProcessDragGesture(eventData);
    }

    private void InitializeController()
    {
        currentPage = FIRST_PAGE;
        targetPos = levelPagesRect.localPosition;
        dragThreshold = Screen.width / DRAG_THRESHOLD_DIVISOR;
    }

    private bool CanNavigateNext()
    {
        return currentPage < maxPage;
    }

    private bool CanNavigatePrevious()
    {
        return currentPage > FIRST_PAGE;
    }

    private void NavigateToNextPage()
    {
        currentPage++;
        targetPos += pageStep;
        MovePage();
    }

    private void NavigateToPreviousPage()
    {
        currentPage--;
        targetPos -= pageStep;
        MovePage();
    }

    private void MovePage()
    {
        levelPagesRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
        UpdateNavigationButtons();
    }

    private void ProcessDragGesture(PointerEventData eventData)
    {
        float dragDistance = Mathf.Abs(eventData.position.x - eventData.pressPosition.x);

        if (IsDragThresholdExceeded(dragDistance))
        {
            HandleSwipeNavigation(eventData);
        }
        else
        {
            MovePage();
        }
    }

    private bool IsDragThresholdExceeded(float dragDistance)
    {
        return dragDistance > dragThreshold;
    }

    private void HandleSwipeNavigation(PointerEventData eventData)
    {
        bool isSwipeRight = eventData.position.x > eventData.pressPosition.x;

        if (isSwipeRight)
        {
            Previous();
        }
        else
        {
            Next();
        }
    }

    private void UpdateNavigationButtons()
    {
        EnableAllButtons();
        DisableButtonsAtBoundaries();
    }

    private void EnableAllButtons()
    {
        nextBtn.interactable = true;
        previousBtn.interactable = true;
    }

    private void DisableButtonsAtBoundaries()
    {
        if (IsAtFirstPage())
        {
            previousBtn.interactable = false;
        }
        else if (IsAtLastPage())
        {
            nextBtn.interactable = false;
        }
    }

    private bool IsAtFirstPage()
    {
        return currentPage == FIRST_PAGE;
    }

    private bool IsAtLastPage()
    {
        return currentPage == maxPage;
    }
}