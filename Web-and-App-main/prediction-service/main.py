from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from model import predict, train_model
import logging

logging.basicConfig(level=logging.INFO)

app = FastAPI(title="AAST Grade Prediction Service", version="1.0.0")

app.add_middleware(CORSMiddleware,
    allow_origins=["*"], allow_methods=["*"], allow_headers=["*"])


class PredictRequest(BaseModel):
    week7Score:    float
    week12Score:   float
    prefinalScore: float


class TrainRequest(BaseModel):
    results: list[dict]


@app.get("/")
def health():
    return {"status": "ok", "service": "grade-prediction"}


@app.post("/predict")
def predict_grades(req: PredictRequest):
    """Predict final exam score and total from mid-semester scores."""
    try:
        return predict(req.week7Score, req.week12Score, req.prefinalScore)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/predict/batch")
def predict_batch(requests: list[PredictRequest]):
    """Predict for multiple students at once (instructor view)."""
    results = []
    for req in requests:
        try:
            results.append(predict(req.week7Score, req.week12Score, req.prefinalScore))
        except Exception as e:
            results.append({"error": str(e)})
    return results


@app.post("/train")
def retrain_model(req: TrainRequest):
    """Retrain model with latest real results from DB."""
    try:
        metrics = train_model(req.results)
        return {"status": "retrained", "metrics": metrics}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
