import base64
import pickle
import io
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import face_recognition
import numpy as np
import cv2

app = FastAPI(title="Face Recognition Service")

# Global dict to store known face encodings
known_face_encodings = {}

@app.on_event("startup")
def load_model():
    global known_face_encodings
    model_path = "face_recognition_model.pkl"
    try:
        with open(model_path, "rb") as f:
            known_face_encodings = pickle.load(f)
        print(f"Model loaded successfully with people: {list(known_face_encodings.keys())}")
    except Exception as e:
        print(f"Error loading model: {e}")
        # Keep empty dict if model doesn't exist yet

class ImagePayload(BaseModel):
    image: str # Base64 encoded string

@app.post("/recognize")
def recognize_face(payload: ImagePayload):
    # Decode base64 image
    try:
        if "," in payload.image:
            # strip data:image/png;base64, prefix if present
            base64_data = payload.image.split(",")[1]
        else:
            base64_data = payload.image
        
        image_bytes = base64.b64decode(base64_data)
        nparr = np.frombuffer(image_bytes, np.uint8)
        # Decode image using OpenCV
        img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        if img is None:
            raise ValueError("Failed to decode image")
        
        # Convert BGR (OpenCV) to RGB (face_recognition)
        rgb_img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"Invalid image format: {e}")

    # Find face locations and encodings
    face_locations = face_recognition.face_locations(rgb_img)
    face_encodings = face_recognition.face_encodings(rgb_img, face_locations)

    matches = []
    tolerance = 0.6
    min_confidence = 0.6

    for (top, right, bottom, left), face_encoding in zip(face_locations, face_encodings):
        matched_name = None
        matched_confidence = 0.0
        
        # Compare with known faces
        for person_name, known_encodings in known_face_encodings.items():
            comp_matches = face_recognition.compare_faces(known_encodings, face_encoding, tolerance=tolerance)
            
            if True in comp_matches:
                face_distances = face_recognition.face_distance(known_encodings, face_encoding)
                best_match_index = np.argmin(face_distances)
                
                if comp_matches[best_match_index]:
                    temp_confidence = 1 - face_distances[best_match_index]
                    if temp_confidence >= min_confidence:
                        matched_name = person_name
                        matched_confidence = temp_confidence
                        break # Break outer loop once a match is found
        
        if matched_name:
            matches.append({
                "studentKey": matched_name,
                "confidence": float(matched_confidence),
                "box": [top, right, bottom, left]
            })

    return {"matches": matches}
