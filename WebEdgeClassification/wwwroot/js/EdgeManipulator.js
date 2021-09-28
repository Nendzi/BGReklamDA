function ShowAllEdges() {
    for (var edge of allEdges) {
        if (edge.edgeType == "kLineSegmentCurve") {
            DrawLine(edge);
        }
        if (edge.edgeType == "kCircularArcCurve") {
            DrawArc(edge);
        }
        if (edge.edgeType == "kEllipticalArcCurve") {
            DrawEllipse(edge);
        }
        if (edge.edgeType == "kCircleCurve") {
            DrawCircle(edge);
        }
    }
}

function Delta(edge, i) {
    return (edge.endPoint[i] - edge.startPoint[i]) * 10;
}

function DrawLine(lineEdge) {
    var dx = Delta(lineEdge, 0);
    var dy = Delta(lineEdge, 1);
    var dz = Delta(lineEdge, 2);

    var heigth = Math.sqrt(dx * dx + dy * dy + dz * dz);

    var geom = new THREE.BufferGeometry().fromGeometry(new THREE.CylinderGeometry(edgeThk, edgeThk, heigth, 16));
    var material = new THREE.MeshBasicMaterial({ color: 0xff0000 });

    var vectorFromLine = new THREE.Vector3(dx, dy, dz);
    vectorFromLine.normalize();

    var quaternion = new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0, 1, 0), vectorFromLine);

    var lineMidPoint = new THREE.Vector3(
        (lineEdge.startPoint[0] + lineEdge.endPoint[0]) / 2 * 10 - 4.39,
        (lineEdge.startPoint[1] + lineEdge.endPoint[1]) / 2 * 10,
        (lineEdge.startPoint[2] + lineEdge.endPoint[2]) / 2 * 10 - 5);

    const transform = new THREE.Matrix4().compose(
        lineMidPoint,
        quaternion,
        new THREE.Vector3(1, 1, 1));

    var fragid =  edgeBuilder.addFragment(geom, material, transform);
    edgeBuilder.changeFragmentsDbId(fragid,lineEdge.id);
}
function DrawArc(arcEdge) {
    var radius = arcEdge.raduis * 10;
    var angle = arcEdge.sweepAngle - arcEdge.startAngle;

    var geom = new THREE.BufferGeometry().fromGeometry(new THREE.TorusGeometry(radius, edgeThk, 16, 100, angle));
    var material = new THREE.MeshBasicMaterial({ color: 0xff0000 });

    var axisArcVec = new THREE.Vector3(
        arcEdge.axisVector[0],
        arcEdge.axisVector[1],
        arcEdge.axisVector[2]
    );
    var refArcVec = new THREE.Vector3(
        arcEdge.refVector[0],
        arcEdge.refVector[1],
        arcEdge.refVector[2]
    );
    var quaternion = new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(1, 0, 0), refArcVec);

    var axisAngle = axisArcVec.angleTo(new THREE.Vector3(0, 0, 1));
    var quat1 = new THREE.Quaternion().setFromAxisAngle(refArcVec, axisAngle);

    quat1.multiply(quaternion);

    var arcCenter = new THREE.Vector3(
        arcEdge.center[0] * 10 - 4.39,
        arcEdge.center[1] * 10,
        arcEdge.center[2] * 10 - 5);

    const transform = new THREE.Matrix4().compose(
        arcCenter,
        quat1,
        new THREE.Vector3(1, 1, 1));

    var fragid = edgeBuilder.addFragment(geom, material, transform);
    edgeBuilder.changeFragmentsDbId(fragid, arcEdge.id);
}
function DrawEllipse(ellipseEdge) {

    const ellipse = new EllipseCurve(
        ellipseEdge.majorRaduis * 10,
        ellipseEdge.minorRaduis * 10,
        ellipseEdge.startAngle,
        ellipseEdge.sweepAngle);

    var geom = new THREE.BufferGeometry().fromGeometry(new THREE.TubeGeometry(ellipse, 20, edgeThk, 16, false));
    var material = new THREE.MeshBasicMaterial({ color: 0xff0000 });

    var ellipseCentarPoint = new THREE.Vector3(
        ellipseEdge.center[0] * 10 - 4.39,
        ellipseEdge.center[1] * 10,
        ellipseEdge.center[2] * 10 - 5);

    const majorAxisVec = new THREE.Vector3(
        ellipseEdge.majorAxis[0],
        ellipseEdge.majorAxis[1],
        ellipseEdge.majorAxis[2]);
    var quaternion = new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(1, 0, 0), majorAxisVec);

    const minorAxisVec = new THREE.Vector3(
        ellipseEdge.minorAxis[0],
        ellipseEdge.minorAxis[1],
        ellipseEdge.minorAxis[2]);
    var quat1 = new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0, 1, 0).applyQuaternion(quaternion), minorAxisVec);
    quat1.multiply(quaternion);

    const transform = new THREE.Matrix4().compose(
        ellipseCentarPoint, quat1, new THREE.Vector3(1, 1, 1));

    var fragid = edgeBuilder.addFragment(geom, material, transform);
    edgeBuilder.changeFragmentsDbId(fragid, ellipseEdge.id);
}
function DrawCircle(circleEdge) {
    var radius = circleEdge.raduis * 10;
    var angle = 2 * Math.PI;

    var geom = new THREE.BufferGeometry().fromGeometry(new THREE.TorusGeometry(radius, edgeThk, 16, 100, angle));
    var material = new THREE.MeshBasicMaterial({ color: 0xff0000 });

    var axisArcVec = new THREE.Vector3(
        circleEdge.axisVector[0],
        circleEdge.axisVector[1],
        circleEdge.axisVector[2]
    );
    var quaternion = new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0, 0, 1), axisArcVec);
       
    var arcCenter = new THREE.Vector3(
        circleEdge.center[0] * 10 - 4.39,
        circleEdge.center[1] * 10,
        circleEdge.center[2] * 10 - 5);

    const transform = new THREE.Matrix4().compose(
        arcCenter,
        quaternion,
        new THREE.Vector3(1, 1, 1));

    var fragid = edgeBuilder.addFragment(geom, material, transform);
    edgeBuilder.changeFragmentsDbId(fragid, circleEdge.id);
}

class EllipseCurve extends THREE.Curve {
    constructor(xRadius, yRadius, aStartAngle, aEndAngle) {
        super();
        this.majorRadius = xRadius;
        this.minorRadius = yRadius;
        this.startAngle = aStartAngle;
        this.endAngle = aEndAngle;
    }

    getPoint(t, optionalTarget = new THREE.Vector3()) {

        var param = this.startAngle + t * this.endAngle;
        var tx = this.majorRadius * Math.cos(param);
        var ty = this.minorRadius * Math.sin(param);
        var tz = 0;

        return optionalTarget.set(tx, ty, tz);
    }
}