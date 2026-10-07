from fastapi import APIRouter, Depends

from app.api.dependencies import require_internal_api_key
from app.api.v1 import coach, labels, meals, plans, workouts

api_router = APIRouter(dependencies=[Depends(require_internal_api_key)])
api_router.include_router(coach.router)
api_router.include_router(plans.router)
api_router.include_router(meals.router)
api_router.include_router(labels.router)
api_router.include_router(workouts.router)
