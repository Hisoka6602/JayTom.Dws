FROM python:3.12-slim
WORKDIR /fixture
COPY fixture.py /fixture/fixture.py
ENV PYTHONUNBUFFERED=1
EXPOSE 18086 19001 19002
ENTRYPOINT ["python", "-u", "/fixture/fixture.py"]
