import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestRegressor
from sklearn.metrics import mean_absolute_error, r2_score
import joblib
import os

MODEL_PATH = "grade_model.joblib"


def generate_training_data():
    np.random.seed(42)
    n = 500

    week7    = np.random.randint(10, 31, n)   # /30
    week12   = np.random.randint(8,  21, n)   # /20
    prefinal = np.random.randint(4,  11, n)   # /10

    base_final = (
        (week7 / 30) * 15 +
        (week12 / 20) * 12 +
        (prefinal / 10) * 8 +
        np.random.normal(0, 3, n)
    ).clip(0, 40).astype(int)

    return pd.DataFrame({
        'week7':    week7,
        'week12':   week12,
        'prefinal': prefinal,
        'final':    base_final,
        'total':    week7 + week12 + prefinal + base_final
    })


def train_model(real_data: list[dict] = None):
    """Train on synthetic data + any real results passed in."""
    df = generate_training_data()

    if real_data and len(real_data) >= 3:
        real_df = pd.DataFrame(real_data)
        real_df = real_df.rename(columns={
            'week7Score':    'week7',
            'week12Score':   'week12',
            'prefinalScore': 'prefinal',
            'finalScore':    'final',
            'totalScore':    'total'
        })
        real_df = real_df[['week7', 'week12', 'prefinal', 'final', 'total']].dropna()
        real_df = pd.concat([real_df] * 5, ignore_index=True)
        df = pd.concat([df, real_df], ignore_index=True)

    X = df[['week7', 'week12', 'prefinal']].values
    y_final = df['final'].values
    y_total = df['total'].values

    model_final = RandomForestRegressor(n_estimators=100, random_state=42)
    model_final.fit(X, y_final)

    model_total = RandomForestRegressor(n_estimators=100, random_state=42)
    model_total.fit(X, y_total)

    pred_final = model_final.predict(X)
    mae = mean_absolute_error(y_final, pred_final)
    r2  = r2_score(y_final, pred_final)

    joblib.dump({'final': model_final, 'total': model_total}, MODEL_PATH)
    return {'mae': round(mae, 2), 'r2': round(r2, 3), 'samples': len(df)}


def load_model():
    if not os.path.exists(MODEL_PATH):
        train_model()
    return joblib.load(MODEL_PATH)


def predict(week7: float, week12: float, prefinal: float) -> dict:
    models = load_model()
    X = np.array([[week7, week12, prefinal]])

    pred_final = float(models['final'].predict(X)[0])
    pred_total = float(models['total'].predict(X)[0])

    pred_final = round(max(0, min(40, pred_final)), 1)
    pred_total = round(max(0, min(100, pred_total)), 1)

    return {
        'predictedFinal': pred_final,
        'predictedTotal': pred_total,
        'finalRange': {
            'low':  max(0,   round(pred_final - 3, 1)),
            'high': min(40,  round(pred_final + 3, 1))
        },
        'totalRange': {
            'low':  max(0,   round(pred_total - 5, 1)),
            'high': min(100, round(pred_total + 5, 1))
        },
        'atRisk': pred_final < 12,
        'riskLevel': (
            'HIGH'   if pred_final < 10 else
            'MEDIUM' if pred_final < 16 else
            'LOW'
        )
    }
