using UnityEngine;
public class SpawnArea : MonoBehaviour
{
    public GameObject notePrefab;
    public Transform hitArea;

    public void SpawnNote()
    {
        GameObject newNote = Instantiate(notePrefab, transform.position, transform.rotation, GetNoteParent());

        BaseNote notes = newNote.GetComponent<BaseNote>();
        if (notes != null && hitArea != null)
        {
            notes.SetTarget(hitArea);
        }
    }

    public void SpawnRotateNote(float angle)
    {
        GameObject newNote = Instantiate(notePrefab, transform.position, transform.rotation, GetNoteParent());

        RotateNote rotateNote = newNote.GetComponent<RotateNote>();
        if (rotateNote != null && hitArea != null)
        {
            rotateNote.SetTarget(hitArea);
            rotateNote.InitializeSnap(angle);
        }
    }

    private Transform GetNoteParent()
    {
        if (hitArea != null)
        {
            TrackManager trackManager = hitArea.GetComponentInParent<TrackManager>();
            if (trackManager != null)
            {
                return trackManager.transform;
            }
        }

        TrackManager parentTrackManager = GetComponentInParent<TrackManager>();
        return parentTrackManager != null ? parentTrackManager.transform : null;
    }
}
