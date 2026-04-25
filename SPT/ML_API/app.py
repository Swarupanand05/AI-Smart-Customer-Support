from flask import Flask, request, jsonify
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.naive_bayes import MultinomialNB

app = Flask(__name__)

# 🔹 Sample training data (you can expand later)
texts = [
    "payment failed", "refund not received",
    "app crash error", "system bug issue",
    "login problem", "password reset issue"
]

categories = [
    "Billing", "Billing",
    "Technical", "Technical",
    "Account", "Account"
]

sentiments = [
    "Negative", "Negative",
    "Negative", "Negative",
    "Neutral", "Neutral"
]

# 🔹 Train models
vectorizer = TfidfVectorizer()
X = vectorizer.fit_transform(texts)

category_model = MultinomialNB()
category_model.fit(X, categories)

sentiment_model = MultinomialNB()
sentiment_model.fit(X, sentiments)


@app.route("/predict", methods=["POST"])
def predict():
    data = request.json
    message = data["message"]

    X_input = vectorizer.transform([message])

    category = category_model.predict(X_input)[0]
    sentiment = sentiment_model.predict(X_input)[0]

    return jsonify({
        "category": category,
        "sentiment": sentiment
    })


if __name__ == "__main__":
    app.run(port=5001)