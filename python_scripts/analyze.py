import pandas as pd
import sys

def analyze(data_path):
    df = pd.read_csv(data_path)
    description = df.describe()
    return description.to_string()

if __name__ == "__main__":
    data_path = sys.argv[1]  # Получаем путь к CSV-файлу из аргументов командной строки
    result = analyze(data_path)
    print(result)