from functools import lru_cache
from pathlib import Path

PROMPTS_ROOT = Path(__file__).resolve().parent


@lru_cache(maxsize=32)
def load_prompt(relative_path: str) -> str:
    """Load a versioned prompt from the prompts directory."""
    prompt_path = (PROMPTS_ROOT / relative_path).resolve()
    if PROMPTS_ROOT not in prompt_path.parents:
        raise ValueError("Prompt path must stay inside prompts directory.")
    return prompt_path.read_text(encoding="utf-8").strip()
